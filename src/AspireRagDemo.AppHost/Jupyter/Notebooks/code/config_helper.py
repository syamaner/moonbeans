import os
import sys
# allow loading modules from local directory.
sys.path.insert(1, '/home/jovyan/work/code')

from config import VectorDBConfig, ModelConfig
from model_provider import ModelProvider

class ConfigHelper:
    def __init__(self, is_eval=False):
        if is_eval:
            self.vector_store_collection_name = f'eval-{os.getenv("ModelConfiguration__VectorStoreCollectionName")}'
        else:            
            self.vector_store_collection_name = os.getenv("ModelConfiguration__VectorStoreCollectionName")
        self.__parse_embedding_configuration()
        self.__parse_vector_store_configuration()
        self.__parse_chat_configuration()

    @property
    def vector_db_config(self):
        return self._vector_db_config
    
    @property
    def embedding_config(self):
        return self._embedding_config
    
    @property
    def chat_config(self):
        return self._chat_config
        
    def __parse_embedding_configuration(self):
        embedding_model: str = os.getenv('ModelConfiguration__EmbeddingModel')
        embedding_model_provider: str = os.getenv('ModelConfiguration__EmbeddingModelProvider')
        embedding_model_provider_api_key:str = os.getenv('ModelConfiguration__EmbeddingModelProviderApiKey')
        model_base_url:str = ''
        try:
            provider = ModelProvider[embedding_model_provider]
        except KeyError:
            print(f"Invalid model provider: {embedding_model_provider}")
            
        if provider == ModelProvider.OllamaHost or provider == ModelProvider.Ollama:
            model_base_url:str =self.__get_endpoint_from_connection(os.getenv('ConnectionStrings__embedding-model'))
        self._embedding_config: ModelConfig = ModelConfig(
            model_name=embedding_model,
            base_url=model_base_url,
            model_provider=provider,
            api_key=embedding_model_provider_api_key
        )
 
    def __parse_chat_configuration(self):
        chat_model = os.getenv('ModelConfiguration__ChatModel')
        chat_model_provider = os.getenv('ModelConfiguration__ChatModelProvider')
        chat_model_provider_api_key= os.getenv('ModelConfiguration__ChatModelProviderApiKey')
        model_base_url:str = ''
        try:
            provider = ModelProvider[chat_model_provider]
        except KeyError:
            print(f"Invalid model provider: {chat_model_provider}")
        if provider == ModelProvider.OllamaHost or provider == ModelProvider.Ollama:
            model_base_url=self.__get_endpoint_from_connection(os.getenv('ConnectionStrings__chat-model'))
            
        self._chat_config: ModelConfig = ModelConfig(
            model_name=chat_model,
            base_url=model_base_url,
            model_provider=provider,
            api_key=chat_model_provider_api_key
        ) 
    
    def __parse_vector_store_configuration(self):
        qdrant_conn = os.getenv('ConnectionStrings__qdrant_http')
        parts = qdrant_conn.split(';')
        qdrant_url = next(p.split('=')[1] for p in parts if p.startswith('Endpoint='))
        qdrant_key = next(p.split('=')[1] for p in parts if p.startswith('Key='))
        vector_store_vector_name = os.getenv('ModelConfiguration__VectorStoreVectorName')
        self._vector_db_config = VectorDBConfig(
            url=qdrant_url,
            api_key=qdrant_key,
            collection_name=self.vector_store_collection_name,
            vector_name=vector_store_vector_name
        )
    
    def __get_endpoint_from_connection(self, conn_str):
        parts = conn_str.split(';')
        return next(p.split('=')[1] for p in parts if p.startswith('Endpoint='))