import os
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.resources import SERVICE_NAME, Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor

from opentelemetry._logs import set_logger_provider
from opentelemetry.exporter.otlp.proto.grpc._log_exporter import (
    OTLPLogExporter
)
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor

import logging


resource = Resource(attributes={
    SERVICE_NAME:  os.getenv('OTEL_SERVICE_NAME', 'streamlit-ui')
})
def get_otlp_exporter():   
    return OTLPSpanExporter(endpoint=os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT"))

def get_tracer():
    span_exporter = get_otlp_exporter()
    # Service name is required for most backends

    provider = TracerProvider(resource=resource)
    processor = BatchSpanProcessor(span_exporter)
    provider.add_span_processor(processor)
    trace.set_tracer_provider(provider)

    return trace.get_tracer(__name__)

def get_logger():
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
    
    return logging.getLogger(__name__)
