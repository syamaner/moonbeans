import os
import logging
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.resources import SERVICE_NAME, Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.instrumentation.langchain import LangchainInstrumentor
from opentelemetry._logs import set_logger_provider
from opentelemetry.exporter.otlp.proto.grpc._log_exporter import (
    OTLPLogExporter
)
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
 
from opentelemetry.instrumentation.qdrant import QdrantInstrumentor

_telemetryInitialised = False
_loggingInitialised = False

resource = Resource(attributes={
    SERVICE_NAME:  os.getenv('OTEL_SERVICE_NAME', 'jupyter-notebook')
})

def get_otlp_exporter():   
    return OTLPSpanExporter(endpoint=os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT"))

def get_tracer():
    global _telemetryInitialised
    if _telemetryInitialised == True:
        return trace.get_tracer(__name__)
    
    _telemetryInitialised = True
    span_exporter = get_otlp_exporter()
    processor = BatchSpanProcessor(span_exporter)
    provider = TracerProvider(resource=resource)
    provider.add_span_processor(processor)
    trace.set_tracer_provider(provider)
    LangchainInstrumentor().instrument()
    QdrantInstrumentor().instrument()
    return trace.get_tracer(__name__)

def get_logger():
    global _loggingInitialised
    if _loggingInitialised == True:
        return logging.getLogger(__name__)
    
    _loggingInitialised = True
    logger_provider = LoggerProvider(
        resource=resource
    )
    set_logger_provider(logger_provider)    
    exporter = OTLPLogExporter(insecure=True)
    logger_provider.add_log_record_processor(BatchLogRecordProcessor(exporter))
    handler = LoggingHandler(level=logging.NOTSET, logger_provider=logger_provider)

    logging.getLogger().addHandler(handler)    
    logging.root.setLevel(logging.INFO)
    # Suppress logging from specific libraries
    logging.getLogger("requests").setLevel(logging.WARNING)
    logging.getLogger("urllib3").setLevel(logging.WARNING)
    logging.getLogger("httpx").setLevel(logging.WARNING)
    
    logger = logging.getLogger(__name__)
    logger.handlers = list(
        filter(
            lambda handler: not isinstance(handler, logging.StreamHandler), 
            logger.handlers
        )
    )
    return logger
