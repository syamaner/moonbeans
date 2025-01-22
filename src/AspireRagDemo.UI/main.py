import streamlit as st
import requests
import os
from dotenv import load_dotenv

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
    def call_custom_api(endpoint, query):
        try:
            response = requests.get(f"{API_BASE_URL}/{endpoint}", params={'query': query})
            response.raise_for_status()
            return response.json()
        except requests.exceptions.RequestException as e:
            st.error(f"Error calling API: {str(e)}")
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
