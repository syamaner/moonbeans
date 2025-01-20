from typing import List, Dict
from langchain_ollama import ChatOllama
from langchain.prompts import PromptTemplate
from langchain.schema.runnable import RunnablePassthrough
from langchain.schema import StrOutputParser
from langchain.prompts import ChatPromptTemplate
from typing import List, Dict
from langchain_ollama import ChatOllama
from langchain.prompts import PromptTemplate
from langchain.schema.runnable import RunnablePassthrough
from langchain.schema import StrOutputParser
from langchain.prompts import ChatPromptTemplate
from config import VectorDBConfig, EmbeddingConfig, ProcessingConfig, CodeEntity, ChatConfig


from qdrant_client import QdrantClient

from langchain_qdrant import QdrantVectorStore
from langchain_ollama import OllamaEmbeddings

import logging
from opentelemetry import trace
logging.basicConfig()
logging.root.setLevel(logging.INFO)
from opentelemetry.instrumentation.langchain import LangchainInstrumentor
LangchainInstrumentor().instrument()

class LocalRAG:
    """A class to handle local RAG operations using Ollama for both embeddings and LLM."""
    
    def __init__(self, 
        vector_db_config: VectorDBConfig,
        embedding_config: EmbeddingConfig,
        chat_config: ChatConfig):
        """
        Initialize the LocalRAG with vector store and model configurations.
        
        Args:
            vector_store: Initialized QdrantVectorStore
            model_name: Name of the Ollama model to use (default: "phi3.5")
        """
        self.logger = logging.getLogger("LocalRAG")
        self.tracer = trace.get_tracer("LocalRAG")
        
        self.collection_name = vector_db_config.collection_name
        self.qdrant = QdrantClient(url=vector_db_config.url, api_key=vector_db_config.api_key)
          
        # Initialize embeddings
        self.embeddings = OllamaEmbeddings(
            model=embedding_config.model_name,
            base_url=embedding_config.base_url
        ) 
        
        self.llm = ChatOllama(model=chat_config.model_name, base_url=chat_config.base_url)
        self.vector_store = QdrantVectorStore(client=self.qdrant,
            collection_name=self.collection_name,
            embedding=self.embeddings)
            
        # Define a better prompt template for RAG
        self.template = """You are a helpful AI assistant specialised in technical questions and good at utilising additional technical resources provided to you as additional context.
        Use the following context to answer the question. 
        If you cannot find the answer in the context, say "I cannot find the answer in the provided context."
        
        Context:
        {context}
        
        Question:
        {question}
        
        Answer:"""
        
        self.prompt = PromptTemplate(
            template=self.template,
            input_variables=["context", "question"]
        )
        
    def format_docs(self, docs: List[Dict]) -> str:
        """Format the retrieved documents into a string."""
        with self.tracer.start_as_current_span("rag format_docs"):
            return "\n\n".join(doc.page_content for doc in docs)
    
    def retrieve_and_answer(self, question: str, k: int = 15) -> str:
        """
        Retrieve relevant documents and generate an answer.
        
        Args:
            question: User's question
            k: Number of documents to retrieve (default: 15)
            
        Returns:
            str: Generated answer
        """
        # First retrieve the documents
        retrieved_docs = self.vector_store.similarity_search(question, k=k)
        formatted_context = self.format_docs(retrieved_docs)
        
        # Create and execute the RAG chain
        chain = (
            self.prompt | 
            self.llm | 
            StrOutputParser()
        )
        
        # Execute the chain with the prepared context and question
        response = chain.invoke({
            "context": formatted_context,
            "question": question
        })
        
        return response
    
    def get_relevant_chunks(self, question: str, k: int = 15) -> List[Dict]:
        """
        Get the relevant chunks for a question without generating an answer.
        Useful for debugging and understanding what context is being used.
        
        Args:
            question: User's question
            k: Number of documents to retrieve (default: 5)
            
        Returns:
            List[Dict]: List of relevant documents
        """
        return self.vector_store.similarity_search(question, k=k)
