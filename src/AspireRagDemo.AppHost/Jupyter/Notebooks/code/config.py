"""Configuration module for document processing pipeline."""
from dataclasses import dataclass
from typing import Optional, Dict, Any, List
from pathlib import Path

@dataclass
class CodeEntity:
    """Represents a code entity (function, class, etc.) with its metadata."""
    name: str
    type: str  # 'function', 'class', 'method'
    docstring: Optional[str]
    start_line: int
    end_line: int
    decorators: List[str]
    parent: Optional[str]
    dependencies: List[str]


@dataclass 
class VectorDBConfig:
    """Configuration for vector database connection."""
    url: str
    api_key: str
    collection_name: str

@dataclass
class EmbeddingConfig:
    """Configuration for embedding model."""
    model_name: str = "mxbai-embed-large"
    base_url: str = "http://ollama:11434"

@dataclass
class ChatConfig:
    """Configuration for embedding model."""
    model_name: str = "phi3.5"
    base_url: str = "http://ollama:11434"
    
@dataclass
class ProcessingConfig:
    """Configuration for document processing."""
    chunk_size: int = 500
    chunk_overlap: int = 50
    add_metadata: bool = True
    extract_code_entities: bool = True