import re
import ast
from typing import Dict, List, Optional
from dataclasses import dataclass
from pathlib import Path
import os
from gitingest import ingest
from langchain_qdrant import QdrantVectorStore
from langchain_ollama import OllamaEmbeddings
from langchain.text_splitter import (
    RecursiveCharacterTextSplitter,
    MarkdownHeaderTextSplitter,
    Language,
    PythonCodeTextSplitter
)
 
import nest_asyncio
import asyncio
from qdrant_client import QdrantClient
import yaml
import json

from config import VectorDBConfig, EmbeddingConfig, ProcessingConfig, CodeEntity
class MetadataExtractor:
    """Extracts rich metadata from different file types."""
    
    @staticmethod
    def extract_python_entities(content: str) -> List[CodeEntity]:
        """Extract functions, classes, and methods from Python code."""
        try:
            tree = ast.parse(content)
            entities = []
            
            for node in ast.walk(tree):
                if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
                    # Extract docstring
                    docstring = ast.get_docstring(node)
                    
                    # Get decorators
                    decorators = [
                        ast.unparse(decorator).strip()
                        for decorator in node.decorator_list
                    ]
                    
                    # Find imports and dependencies
                    dependencies = []
                    for sub_node in ast.walk(node):
                        if isinstance(sub_node, ast.Import):
                            dependencies.extend(n.name for n in sub_node.names)
                        elif isinstance(sub_node, ast.ImportFrom):
                            dependencies.append(sub_node.module)
                    
                    entity = CodeEntity(
                        name=node.name,
                        type='class' if isinstance(node, ast.ClassDef) else 'function',
                        docstring=docstring,
                        start_line=node.lineno,
                        end_line=node.end_lineno,
                        decorators=decorators,
                        parent=None,  # Will be filled later for methods
                        dependencies=list(set(dependencies))
                    )
                    entities.append(entity)
            
            return entities
        except SyntaxError:
            return []

    @staticmethod
    def extract_markdown_metadata(content: str) -> Dict:
        """Extract metadata from markdown files."""
        metadata = {
            'headers': [],
            'links': [],
            'code_blocks': [],
            'frontmatter': None
        }
        
        # Extract headers
        headers = re.findall(r'^(#{1,6})\s+(.+)$', content, re.MULTILINE)
        metadata['headers'] = [(len(h[0]), h[1]) for h in headers]
        
        # Extract links
        links = re.findall(r'\[([^\]]+)\]\(([^\)]+)\)', content)
        metadata['links'] = links
        
        # Extract code blocks
        code_blocks = re.findall(r'```(\w+)?\n(.*?)```', content, re.DOTALL)
        metadata['code_blocks'] = [(lang or 'text', code) for lang, code in code_blocks]
        
        # Extract frontmatter
        if content.startswith('---'):
            try:
                fm_match = re.match(r'---\n(.*?)\n---', content, re.DOTALL)
                if fm_match:
                    metadata['frontmatter'] = yaml.safe_load(fm_match.group(1))
            except yaml.YAMLError:
                pass
        
        return metadata

    @staticmethod
    def extract_html_template_metadata(content: str, file_type: str) -> Dict:
        """Extract metadata from HTML/Jinja templates."""
        metadata = {
            'blocks': [],
            'extends': None,
            'includes': [],
            'macros': [],
            'variables': []
        }
        
        if file_type == '.jinja':
            # Extract template inheritance
            extends_match = re.search(r'{%\s*extends\s+[\'"](.+?)[\'"]', content)
            if extends_match:
                metadata['extends'] = extends_match.group(1)
            
            # Extract blocks
            blocks = re.findall(r'{%\s*block\s+(\w+)\s*%}', content)
            metadata['blocks'] = blocks
            
            # Extract includes
            includes = re.findall(r'{%\s*include\s+[\'"](.+?)[\'"]', content)
            metadata['includes'] = includes
            
            # Extract macros
            macros = re.findall(r'{%\s*macro\s+(\w+)\s*\(', content)
            metadata['macros'] = macros
            
            # Extract variables
            variables = re.findall(r'{{(.+?)}}', content)
            metadata['variables'] = [v.strip() for v in variables]
        
        return metadata
