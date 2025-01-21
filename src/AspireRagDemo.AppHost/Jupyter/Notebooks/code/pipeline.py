"""Main document processing pipeline."""
import logging
from typing import List, Dict, Any, Optional
from pathlib import Path
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
import inspect 
import nest_asyncio
import asyncio
from qdrant_client import QdrantClient
import yaml
import json
from qdrant_client.http import models as rest
# Enable nested event loops
nest_asyncio.apply()

from gitingest import ingest

from config import VectorDBConfig, EmbeddingConfig, ProcessingConfig, CodeEntity
 
from MetadataExtractor import MetadataExtractor


import logging
from opentelemetry import trace
logging.basicConfig()
logging.root.setLevel(logging.INFO)
from opentelemetry.instrumentation.langchain import LangchainInstrumentor
LangchainInstrumentor().instrument()

 
class DocumentPipeline:
    """Main document processing pipeline."""
    
    def __init__(
        self,
        vector_db_config: VectorDBConfig,
        embedding_config: EmbeddingConfig,
        processing_config: ProcessingConfig
    ):
        self.processing_config = processing_config
        
        self.logger = logging.getLogger("IngestionPipeline")
        self.tracer = trace.get_tracer("IngestionPipeline")
        # Initialize vector DB
        self.qdrant = QdrantClient(url=vector_db_config.url, api_key=vector_db_config.api_key)
        self.collection_name = vector_db_config.collection_name
        
        # Initialize embeddings
        self.embeddings = OllamaEmbeddings(
            model=embedding_config.model_name,
            base_url=embedding_config.base_url
        ) 
        #Initialize vector store
        self._setup_vector_store()
        
    def _setup_vector_store(self):
        """Setup vector store collection."""
        with self.tracer.start_as_current_span("setup verctor store"):
            vector_size = len(self.embeddings.embed_query("test"))
            
            if not self.qdrant.collection_exists(self.collection_name):
                self.logger.info(f"Collection {self.collection_name} does not exist. Will create now. Vector size is {vector_size}")
                self.qdrant.create_collection(
                    collection_name=self.collection_name,
                    vectors_config=rest.VectorParams(
                        size=vector_size,
                        distance=rest.Distance.COSINE
                    )
                )
                self.logger.info(f"Created collection: {self.collection_name}")
            else:
                self.logger.info(f"Collection {self.collection_name} already exists")
 
            self.vector_store = QdrantVectorStore(
                client=self.qdrant,
                collection_name=self.collection_name,
                embedding=self.embeddings
            )
    def process_repository(self, repo_url: str):
        """Process a repository and store its documents in the vector store."""
        
        with self.tracer.start_as_current_span("process repository"):
            #print(inspect.getfullargspec(ingest))
            # FullArgSpec(args=['source', 'max_file_size', 'include_patterns', 'exclude_patterns', 'output'],
            summary, tree, content = ingest(repo_url, include_patterns=["*.md","*.yml","*.yaml"])
            self.logger.info(summary)
            # 4. Process and index the repository
            chunks = self.advanced_repository_chunking(content, repo_url)
            # 6. Add documents to vector store
            self.vector_store.add_documents(chunks)
            print("done")
    def process_single_file(self, file_path: str, repo_url: str):
        """Process a file that contains concancated files in the repository."""
        
        with self.tracer.start_as_current_span("process file"):
            # #print(inspect.getfullargspec(ingest))
            # # FullArgSpec(args=['source', 'max_file_size', 'include_patterns', 'exclude_patterns', 'output'],
            # summary, tree, content = ingest(repo_url, include_patterns=["*.md","*.yml","*.yaml"])
            # self.logger.info(summary)
            # 4. Process and index the repository
            with open(file_path, 'r') as file:
                content = file.read()
      
            chunks = self.advanced_repository_chunking(content, repo_url)
            # 6. Add documents to vector store
            self.vector_store.add_documents(chunks)
            print("done")
    def create_chunking_strategies(self) -> Dict[str, RecursiveCharacterTextSplitter]:
        """Create specialized chunking strategies for different file types."""
        return {
            # Python files
            '.py': RecursiveCharacterTextSplitter.from_language(
                language=Language.PYTHON,
                chunk_size=500,
                chunk_overlap=50,
                #separators=["\nclass ", "\ndef ", "\n\n", "\n"]
            ),
            
            # Markdown files
            '.md': MarkdownHeaderTextSplitter(
                headers_to_split_on=[
                    ("#", "Header 1"),
                    ("##", "Header 2"),
                    ("###", "Header 3"),
                    ("####", "Header 4")
                ]
            ),
            
            # HTML/Jinja templates
            '.html': RecursiveCharacterTextSplitter(
                chunk_size=500,
                chunk_overlap=100,
                separators=["</div>", "</template>", "</section>", "\n\n", "\n"]
            ),
            '.jinja': RecursiveCharacterTextSplitter(
                chunk_size=500,
                chunk_overlap=100,
                separators=["{% block ", "{% extends ", "{% include ", "\n\n", "\n"]
            ),
            
            # Config files
            '.yml': RecursiveCharacterTextSplitter(
                chunk_size=300,
                chunk_overlap=50,
                separators=["---", "\n\n", "\n"]
            ),
            '.yaml': RecursiveCharacterTextSplitter(
                chunk_size=300,
                chunk_overlap=50,
                separators=["---", "\n\n", "\n"]
            ),
            '.toml': RecursiveCharacterTextSplitter(
                chunk_size=300,
                chunk_overlap=50,
                separators=["\n\n", "\n"]
            ),
            
            # Other common file types
            '.json': RecursiveCharacterTextSplitter(
                chunk_size=500,
                chunk_overlap=50,
                separators=["},", "}\n", "\n"]
            ),
            '.rst': RecursiveCharacterTextSplitter(
                chunk_size=600,
                chunk_overlap=100,
                separators=["\n=+\n", "\n-+\n", "\n\n", "\n"]
            ),
            '.txt': RecursiveCharacterTextSplitter(
                chunk_size=500,
                chunk_overlap=50,
                separators=["\n\n", "\n", ". "]
            ),
            
            # Default
            'default': RecursiveCharacterTextSplitter(
                chunk_size=400,
                chunk_overlap=50,
                separators=["\n\n", "\n", ". ", " "]
            )
        }
    def extract_file_metadata(self, file_path: str, content: str) -> Dict:
        """Extract comprehensive metadata for a file."""
        
        with self.tracer.start_as_current_span("extract metadata"):
            file_ext = os.path.splitext(file_path)[1].lower()
            metadata = {
                "file_path": file_path,
                "file_type": file_ext,
                "file_name": os.path.basename(file_path),
                "directory": os.path.dirname(file_path),
                "size_bytes": len(content.encode('utf-8')),
                "num_lines": len(content.splitlines()),
                "is_empty": len(content.strip()) == 0,
                "has_shebang": content.startswith('#!') if content else False,
                "file_level_metadata": {}
            }
            
            # Extract file type specific metadata
            if file_ext == '.py':
                python_entities = MetadataExtractor.extract_python_entities(content)
                metadata['file_level_metadata'].update({
                    'classes': [e for e in python_entities if e.type == 'class'],
                    'functions': [e for e in python_entities if e.type == 'function'],
                    'has_main': any(e.name == '__main__' for e in python_entities),
                    'imports': re.findall(r'^(?:from|import)\s+(\S+)', content, re.MULTILINE),
                    'doc_coverage': sum(1 for e in python_entities if e.docstring) / len(python_entities) if python_entities else 0
                })
            
            elif file_ext == '.md':
                metadata['file_level_metadata'].update(
                    MetadataExtractor.extract_markdown_metadata(content)
                )
            
            elif file_ext in ['.html', '.jinja']:
                metadata['file_level_metadata'].update(
                    MetadataExtractor.extract_html_template_metadata(content, file_ext)
                )
            
            elif file_ext in ['.yml', '.yaml']:
                try:
                    yaml_content = yaml.safe_load(content)
                    metadata['file_level_metadata']['yaml_structure'] = {
                        'top_level_keys': list(yaml_content.keys()) if isinstance(yaml_content, dict) else [],
                        'is_list': isinstance(yaml_content, list)
                    }
                except yaml.YAMLError:
                    pass
            
            elif file_ext == '.json':
                try:
                    json_content = json.loads(content)
                    metadata['file_level_metadata']['json_structure'] = {
                        'top_level_keys': list(json_content.keys()) if isinstance(json_content, dict) else [],
                        'is_array': isinstance(json_content, list)
                    }
                except json.JSONDecodeError:
                    pass
            
            return metadata
    # 2. Let's modify split_by_files to be more verbose and handle errors better
    def split_by_files(self, content: str) -> List[Dict]:
        """Split the concatenated content into individual files with their paths."""
        if not content:
            print("Warning: Content is empty")
            return []
        
        with self.tracer.start_as_current_span("split the files"):
            # Split by the file delimiter
            file_parts = content.split("================================================")
            files = []
            current_file = None
            current_content = []
            
            print(f"Total parts found: {len(file_parts)}")
            
            for part in file_parts:
                part = part.strip()
                if not part:
                    continue
                    
                lines = part.split('\n')
                if lines[0].startswith("File: "):
                    # If we have a previous file, save it
                    if current_file:
                        files.append({
                            "path": current_file,
                            "content": "\n".join(current_content),
                            "metadata": self.extract_file_metadata(current_file, "\n".join(current_content))
                        })
                    
                    # Start new file
                    current_file = lines[0].replace("File: ", "").strip()
                    current_content = lines[1:]  # Skip the "File:" line
                else:
                    # This shouldn't normally happen but handle it just in case
                    if current_file:
                        current_content.extend(lines)
            
            # Don't forget to add the last file
            if current_file:
                files.append({
                    "path": current_file,
                    "content": "\n".join(current_content),
                    "metadata": self.extract_file_metadata(current_file, "\n".join(current_content))
                })
            
            print(f"Total files processed: {len(files)}")
            if files:
                print(f"Sample file paths:")
                for i, file in enumerate(files[:3]): # Show first 3 files
                    print(f" {i+1}. {file['path']}")
            
            return files
   
    # 3. Let's modify advanced_repository_chunking to be more verbose
    def advanced_repository_chunking(self, content: str, repo_url: str):
        """Process repository content using file-type specific chunking with rich metadata."""
        
        with self.tracer.start_as_current_span("Start chunking"):
            self.logger.info("Starting repository chunking...")
            
            # Get chunking strategies
            chunking_strategies = self.create_chunking_strategies()
            self.logger.info("Created chunking strategies")
            
            # Split content into files
            files = self.split_by_files(content)
            self.logger.info(f"Split content into {len(files)} files")
            
            if not files:
                self.logger.info("Warning: No files to process")
                return []
            
            # Extract repository-level metadata
            repo_metadata = {
                "repository_url": repo_url,
                "repository_name": repo_url.split('/')[-1],
                "organization": repo_url.split('/')[-2],
                "total_files": len(files),
                "file_types": {},
                "directory_structure": {}
            }
            
            # Process each file with appropriate chunking strategy
            processed_chunks = []
            for file in files:
                file_ext = file["metadata"]["file_type"]
                #print(f"Processing file with extension: {file_ext}")
                
                splitter = chunking_strategies.get(file_ext, chunking_strategies['default'])
                combined_metadata = {
                    **file["metadata"],
                    "repository": repo_metadata
                }
                
                try:
                    if file_ext == '.md':
                        header_splits = splitter.split_text(file["content"])
                        if any(len(split.page_content) > 600 for split in header_splits):
                            size_splitter = RecursiveCharacterTextSplitter(
                                chunk_size=600,
                                chunk_overlap=50,
                                separators=["\n\n", "\n", ". "]
                            )
                            for split in header_splits:
                                smaller_splits = size_splitter.create_documents(
                                    texts=[split.page_content],
                                    metadatas=[{
                                        **split.metadata,
                                        **combined_metadata
                                    }]
                                )
                                processed_chunks.extend(smaller_splits)
                        else:
                            for split in header_splits:
                                split.metadata.update(combined_metadata)
                            processed_chunks.extend(header_splits)
                    else:
                        chunks = splitter.create_documents(
                            texts=[file["content"]],
                            metadatas=[combined_metadata]
                        )
                        processed_chunks.extend(chunks)
                        print(f"Added {len(chunks)} chunks for file {file['path']}")
                except Exception as e:
                    print(f"Error processing file {file['path']}: {str(e)}")
                    continue
            
            self.logger.info(f"Total chunks created: {len(processed_chunks)}")
            #if processed_chunks:
               # print("Sample chunk metadata:", processed_chunks[0].metadata)
            return processed_chunks
