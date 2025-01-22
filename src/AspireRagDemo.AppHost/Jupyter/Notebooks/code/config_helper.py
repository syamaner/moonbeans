import os
import sys
# allow loading modules from local directory.
sys.path.insert(1, '/home/jovyan/work/code')

from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.resources import SERVICE_NAME, Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.instrumentation.langchain import LangchainInstrumentor
# Logging (Experimental)
from opentelemetry._logs import set_logger_provider
from opentelemetry.exporter.otlp.proto.grpc._log_exporter import (
    OTLPLogExporter
)
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
from opentelemetry.sdk.resources import Resource

from config import VectorDBConfig, EmbeddingConfig, ProcessingConfig, ChatConfig

import nest_asyncio
import asyncio 
import logging
 
# Enable nested event loops
nest_asyncio.apply()

# Initialise and Setup OpenTelemetry for the session
resource = Resource(attributes={
  SERVICE_NAME:  os.getenv('OTEL_SERVICE_NAME', 'jupyter-demo')
})
provider = TracerProvider(resource=resource)
processor = BatchSpanProcessor(OTLPSpanExporter(endpoint=os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT")))
provider.add_span_processor(processor)
trace.set_tracer_provider(provider)

logger_provider = LoggerProvider(
    resource=resource
)
set_logger_provider(logger_provider)

exporter = OTLPLogExporter(insecure=True)
logger_provider.add_log_record_processor(BatchLogRecordProcessor(exporter))
handler = LoggingHandler(level=logging.NOTSET, logger_provider=logger_provider)
# Attach OTLP handler to root logger
logging.getLogger().addHandler(handler)

logging.root.setLevel(logging.INFO)

LangchainInstrumentor().instrument()
logger = logging.getLogger(__name__)
tracer = trace.get_tracer(__name__)
logger.handlers = list(
    filter(
        lambda handler: not isinstance(handler, logging.StreamHandler), 
        logger.handlers
    )
)
openAiKey = os.getenv("OPENAI_KEY")
os.environ["OPENAI_API_KEY"] = openAiKey

class ConfigHelper:
    def __init__(self, is_eval=False):
        if is_eval:
            self.vector_store_collection_name = f'1eval-{os.getenv("VECTOR_STORE_COLLECTION_NAME")}'
        else:            
            self.vector_store_collection_name = {os.getenv("VECTOR_STORE_COLLECTION_NAME")}
        self.__parse_configuration()
        
    def parse_ollama_connection(self, conn_str):
        parts = conn_str.split(';')
        endpoint = next(p.split('=')[1] for p in parts if p.startswith('Endpoint='))
        model = next(p.split('=')[1] for p in parts if p.startswith('Model='))
        return endpoint, model
        
    def __parse_configuration(self):
        chat_conn = os.getenv('ConnectionStrings__chat-model')
        chat_model_url, chat_model_id = self.parse_ollama_connection(chat_conn)
        
        embeddings_conn = os.getenv('ConnectionStrings__embedding-model')
        embedding_model_url, embeddings_model = self.parse_ollama_connection(embeddings_conn)
        
        qdrant_conn = os.getenv('ConnectionStrings__qdrant_http')
        parts = qdrant_conn.split(';')
        qdrant_url = next(p.split('=')[1] for p in parts if p.startswith('Endpoint='))
        qdrant_key = next(p.split('=')[1] for p in parts if p.startswith('Key='))
        
        self._vector_db_config = VectorDBConfig(
            url=qdrant_url,
            api_key=qdrant_key,
            collection_name="evaluation-ek-md"
        )
        self._chat_config = ChatConfig(
            model_name=chat_model_id,
            base_url=chat_model_url
        )
        self._embedding_config = EmbeddingConfig(
            model_name=embeddings_model,
            base_url=embedding_model_url
        )
        
    @property
    def vector_db_config(self):
        return self._vector_db_config
    
    @property
    def embedding_config(self):
        return self._embedding_config
    
    @property
    def chat_config(self):
        return self._chat_config

