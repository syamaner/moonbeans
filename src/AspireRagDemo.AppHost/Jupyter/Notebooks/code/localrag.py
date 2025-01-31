from typing import List, Dict
from langchain_ollama import ChatOllama, OllamaEmbeddings
from langchain_openai import ChatOpenAI, OpenAIEmbeddings
from langchain_community.embeddings import ( 
    HuggingFaceInferenceAPIEmbeddings,
)
from langchain_community.chat_models import ( ChatHuggingFace )
from langchain.prompts import PromptTemplate
from langchain.schema import StrOutputParser
from typing import List, Dict
from langchain.prompts import PromptTemplate
from langchain.schema import StrOutputParser
from config import VectorDBConfig, ModelConfig
from qdrant_client import QdrantClient
from langchain_qdrant import QdrantVectorStore
import logging
from opentelemetry import trace
from model_provider import ModelProvider

class LocalRAG:
    """A class to handle local RAG operations using Ollama for both embeddings and LLM."""
    
    def __init__(self, vector_db_config: VectorDBConfig, 
                 embedding_config: ModelConfig, chat_config: ModelConfig):
        self.logger = logging.getLogger("local-rag") #logging.getLogger("IngestionPipeline")
        self.tracer = trace.get_tracer("local-rag")
        
        self.InitEmbeddings(embedding_config)
        self.InitChatModel(chat_config)

        self.qdrant = QdrantClient(url=vector_db_config.url, api_key=vector_db_config.api_key)
        self.vector_store = QdrantVectorStore(
                client=self.qdrant,
                collection_name=vector_db_config.collection_name,
                vector_name=vector_db_config.vector_name,
                embedding=self.embeddings
            )
            
        # Define a better prompt template for RAG
        self.template = """
        You are a helpful AI assistant specialised in technical questions and good at utilising additional technical resources provided to you as additional context.
        Use the following context to answer the question. You pride yourself on bringing necessary references when needed.
        You prefer a good summary over a long explanation but also provide clear justification for the answer.
        Please do not include the question in the answer.
        If you cannot find the answer in the context, please say "I cannot find the answer in the provided context."
        
        Context:
        {context}
        
        Question:
        {question}
        """
        
        self.prompt = PromptTemplate(
            template=self.template,
            input_variables=["context", "question"]
        )
        
    def InitEmbeddings(self, embedding_config: ModelConfig):
        if(embedding_config.model_provider == ModelProvider.HuggingFace):
            self.logger.info(f"Using HuggingFace embedding model: {embedding_config.model_name}")
            self.embeddings = HuggingFaceInferenceAPIEmbeddings(
                api_key=embedding_config.api_key,
                model_name=embedding_config.model_name
            )
        elif(embedding_config.model_provider == ModelProvider.OpenAI):            
            self.logger.info(f"Using OpenAI embedding model: {embedding_config.model_name}")  
            self.embeddings = OpenAIEmbeddings(
                model=embedding_config.model_name,
                api_key=embedding_config.api_key
            )
        else:
            self.logger.info(f"Using Ollama embedding model: {embedding_config.model_name}, base url: {embedding_config.base_url}")
            self.embeddings = OllamaEmbeddings(
                model=embedding_config.model_name,
                base_url=embedding_config.base_url
            )
   
    def InitChatModel(self, chat_config: ModelConfig):
        if(chat_config.model_provider == ModelProvider.HuggingFace):
            self.logger.info(f"Using HuggingFace chat model: {chat_config.model_name}")
            self.llm = ChatHuggingFace(
                api_key=chat_config.api_key,
                model_name=chat_config.model_name
            )
        elif(chat_config.model_provider == ModelProvider.OpenAI):            
            self.logger.info(f"Using OpenAI chat model: {chat_config.model_name}")  
            self.llm = ChatOpenAI(
                model=chat_config.model_name,
                api_key=chat_config.api_key
            )
        else:
            self.logger.info(f"Using Ollama chat model: {chat_config.model_name}, base url: {chat_config.base_url}")
            self.llm = ChatOllama(
                model=chat_config.model_name,
                base_url=chat_config.base_url
            )
            
    def format_docs(self, docs: List[Dict]) -> str:
        """Format the retrieved documents into a string."""
        with self.tracer.start_as_current_span("rag format_docs"):
            return "\n\n".join(doc.page_content for doc in docs)
    
    def retrieve_and_answer(self, question: str, k: int = 15) -> str:
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
