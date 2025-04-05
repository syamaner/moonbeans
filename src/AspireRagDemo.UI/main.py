import streamlit as st
import os
import requests
from dotenv import load_dotenv
from opentelemetry.trace.propagation.tracecontext import TraceContextTextMapPropagator
from TraceSetup import get_tracer, get_logger

tracer = get_tracer()
logger = get_logger()

# Initialize Streamlit page configuration
st.set_page_config(
    page_title="RAG Demo",
    page_icon="🔍",
    layout="wide"
)

# Load environment variables
load_dotenv()

# Configure API endpoint
API_BASE_URL = os.getenv('services__api-service__http__0')

# List of embedding models
EMBEDDING_MODELS = [
    "nomic-embed-text",
    "mxbai-embed-large",
    "snowflake-arctic-embed",
    "granite-embedding",
    "text-embedding-3-large"
]

# List of chat models
CHAT_MODELS = [
    "chatgpt-4o-latest",
    "llama3.2",
    "llama3.2:1b",
    "phi4",
    "mistral",
    "gemma",
    "gemma:2b",
    "phi3:3.8b",
    "phi3:14b",
    "qwen2.5:14b",
    "qwen2.5:32b",
    "deepseek-r1",
    "deepseek-r1:14b",
    "deepseek-r1:32b",
    "llama3.1",
    "llama3.3",
    "deepseek-r1:70b",
    "qwen2.5:72b",
    "llama3.1:70b"
]

def main():
    def call_custom_api(endpoint, query, embedding_model, chat_model):
        try:
            with tracer.start_as_current_span("Call API") as span1:
                carrier = {}
                TraceContextTextMapPropagator().inject(carrier)
                logger.info(carrier)
                header = {"traceparent": carrier["traceparent"]}
                response = requests.get(
                    f"{API_BASE_URL}/{endpoint}",
                    params={
                        'query': query,
                        'chatModel': chat_model,
                        'embeddingModel': embedding_model
                    },
                    headers=header
                )
                response.raise_for_status()
                return response.json()
        except requests.exceptions.RequestException as e:
            st.error(f"Error calling API: {str(e)}")
            logger.error(f"Error calling API: {str(e)}")
            return None

    # Streamlit UI
    st.title("RAG Demo")
    st.write("Enter your question below:")

    # User input
    user_query = st.text_input("Question")

    # Create columns for model selection
    col_models1, col_models2 = st.columns(2)

    # Model selection dropdowns
    with col_models1:
        selected_embedding_model = st.selectbox(
            "Select Embedding Model",
            options=EMBEDDING_MODELS,
            index=0  # Default to first model
        )

    with col_models2:
        selected_chat_model = st.selectbox(
            "Select Chat Model",
            options=CHAT_MODELS,
            index=CHAT_MODELS.index("llama3.3") if "llama3.3" in CHAT_MODELS else 0  # Default to llama3.3 if available
        )

    # Create columns for buttons
    col1, col2 = st.columns(2)

    # Query buttons
    if col1.button('Search with context') and user_query:
        with st.spinner('Searching...'):
            results = call_custom_api('chat-with-context', user_query, selected_embedding_model, selected_chat_model)
            if results:
                st.write("Results:")
                st.write(results)

    if col2.button('Search without context') and user_query:
        with st.spinner('Searching...'):
            results = call_custom_api('chat', user_query, selected_embedding_model, selected_chat_model)
            if results:
                st.write("Results:")
                st.write(results)

if __name__ == '__main__':
    main()