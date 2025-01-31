"""Configuration module for document processing pipeline."""
from dataclasses import dataclass
from model_provider import ModelProvider
@dataclass 
class VectorDBConfig:
    """Configuration for vector database connection."""
    url: str
    api_key: str
    collection_name: str
    vector_name: str = "page_content_vector"

@dataclass
class ModelConfig:
    """Configuration for embedding / chat model."""
    model_name: str = ""
    base_url: str = ""
    model_provider: ModelProvider = ModelProvider.Ollama
    api_key: str = ""