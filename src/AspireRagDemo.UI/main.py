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

def main():
  #  @tracer.start_as_current_span("call_custom_api")
    def call_custom_api(endpoint, query):
        try:
            with tracer.start_as_current_span("Call API") as span1:
                carrier = {}
                TraceContextTextMapPropagator().inject(carrier)
                logger.info(carrier)
                header = {"traceparent": carrier["traceparent"]}    
                response = requests.get(f"{API_BASE_URL}/{endpoint}", params={'query': query}, headers=header)
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

    # Query button
    if st.button('Search with context') and user_query:
        with st.spinner('Searching...'):
            results = call_custom_api('chat-with-context', user_query)
            if results:
                st.write("Results:")
                st.write(results)
    # Query button
    if st.button('Search without context') and user_query:
        with st.spinner('Searching...'):
            results = call_custom_api('chat', user_query)
            if results:
                st.write("Results:")
                st.write(results)
if __name__ == '__main__':
    main()
