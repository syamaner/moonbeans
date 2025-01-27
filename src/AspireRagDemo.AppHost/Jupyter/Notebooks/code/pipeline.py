from typing import List, Dict, Optional
from pathlib import Path
import os
import re
import logging
from langchain_qdrant import QdrantVectorStore
from langchain_ollama import OllamaEmbeddings
from langchain.text_splitter import (
    RecursiveCharacterTextSplitter,
    MarkdownHeaderTextSplitter,
)
from qdrant_client import QdrantClient
from qdrant_client.http import models as rest
import yaml
import json
from gitingest import ingest
from config import VectorDBConfig, EmbeddingConfig
from opentelemetry import trace

class DocumentPipeline:
    """Main document processing pipeline."""
    
    def __init__(
        self,
        vector_db_config: VectorDBConfig,
        embedding_config: EmbeddingConfig
    ):        
        self.logger = logging.getLogger("IngestionPipeline")
        self.tracer = trace.get_tracer("IngestionPipeline")
        self.collection_name = vector_db_config.collection_name
        # Initialize embeddings
        self.embeddings = OllamaEmbeddings(
            model=embedding_config.model_name,
            base_url=embedding_config.base_url
        ) 
        self.qdrant = QdrantClient(url=vector_db_config.url, api_key=vector_db_config.api_key)
        self.__setup_vector_store()

    def process_repository(self, repo_url: str):
        """Process a repository and store its documents in the vector store."""        
        with self.tracer.start_as_current_span("process repository"):
            summary, tree, content = ingest(repo_url, include_patterns=["*.md","*.yml","*.yaml"])
            self.logger.info(summary)
            chunks = self.__chunk_content(content, repo_url)
            self.vector_store.add_documents(chunks)
            print("done processing the repository.")
            self.logger.info("done processing the repository.")
            
            
    def process_single_file(self, file_path: str, repo_url: str):
        """Process a file that contains concancated files in the repository."""
       
        with self.tracer.start_as_current_span("process file"):
            with open(file_path, 'r') as file:
                content = file.read()
            chunks = self.__chunk_content(content, repo_url)
            self.vector_store.add_documents(chunks)
            print("done processing the repository.")
            self.logger.info("done processing the repository.")

    def __chunk_content(self, content: str, repo_url: str):
        """Process repository content using file-type specific chunking with rich metadata."""
        
        with self.tracer.start_as_current_span("Start chunking"):
            self.logger.info("Starting repository chunking...")
            
            # Get chunking strategies
            chunking_strategies = self.__create_chunking_strategies()
            self.logger.info("Created chunking strategies")
            
            # Split content into files
            files = self.__split_by_files(content)
            self.logger.info(f"Split content into {len(files)} files")
            
            if not files:
                self.logger.info("Warning: No files to process")
                return []
            
            # Extract repository-level metadata
            repo_metadata = {
                "repository_url": repo_url,
                "repository_name": repo_url.split('/')[-1],
                "repository_organization": repo_url.split('/')[-2],
                "repository_total_files": len(files),
                "repository_file_types": {},
                "repository_directory_structure": {}
            }            
            # Process each file with appropriate chunking strategy
            processed_chunks = []
            for file in files:
                file_ext = file["metadata"]["file_type"]
                
                splitter = chunking_strategies.get(file_ext, chunking_strategies['default'])
                combined_metadata = {
                    **file["metadata"],
                    **repo_metadata
                }
                
                try:
                    if file_ext == '.md':
                        header_splits = splitter.split_text(file["content"])
                        if any(len(split.page_content) > 600 for split in header_splits):
                            size_splitter = RecursiveCharacterTextSplitter(
                                chunk_size=600,
                                chunk_overlap=50,
                                separators=["\n\n", "\n", ". "]
                            )
                            for split in header_splits:
                                smaller_splits = size_splitter.create_documents(
                                    texts=[split.page_content],
                                    metadatas=[{
                                        **split.metadata,
                                        **combined_metadata
                                    }]
                                )
                                processed_chunks.extend(smaller_splits)
                        else:
                            for split in header_splits:
                                split.metadata.update(combined_metadata)
                            processed_chunks.extend(header_splits)
                    else:
                        chunks = splitter.create_documents(
                            texts=[file["content"]],
                            metadatas=[combined_metadata]
                        )
                        processed_chunks.extend(chunks)
                        print(f"Added {len(chunks)} chunks for file {file['path']}")
                except Exception as e:
                    print(f"Error processing file {file['path']}: {str(e)}")
                    continue
            
            self.logger.info(f"Total chunks created: {len(processed_chunks)}")
            return processed_chunks
      
    def __split_by_files(self, content: str) -> List[Dict]:
        """Split the concatenated content into individual files with their paths."""
        if not content:
            print("Warning: Content is empty")
            return []
        
        with self.tracer.start_as_current_span("split the files"):
            # Split by the file delimiter
            file_parts = content.split("================================================")
            files = []
            current_file = None
            current_content = []
            
            print(f"Total parts found: {len(file_parts)}")
            
            for part in file_parts:
                part = part.strip()
                if not part:
                    continue
                    
                lines = part.split('\n')
                if lines[0].startswith("File: "):
                    # If we have a previous file, save it
                    if current_file:
                        files.append({
                            "path": current_file,
                            "content": "\n".join(current_content),
                            "metadata": self.__extract_file_metadata(current_file, "\n".join(current_content))
                        })
                    
                    # Start new file
                    current_file = lines[0].replace("File: ", "").strip()
                    current_content = lines[1:]  # Skip the "File:" line
                else:
                    # This shouldn't normally happen but handle it just in case
                    if current_file:
                        current_content.extend(lines)
            
            # Don't forget to add the last file
            if current_file:
                files.append({
                    "path": current_file,
                    "content": "\n".join(current_content),
                    "metadata": self.__extract_file_metadata(current_file, "\n".join(current_content))
                })
            
            print(f"Total files processed: {len(files)}")
            if files:
                print(f"Sample file paths:")
                for i, file in enumerate(files[:3]): # Show first 3 files
                    print(f" {i+1}. {file['path']}")
            
            return files
            
    def __extract_file_metadata(self, file_path: str, content: str) -> Dict:
        """Extract comprehensive metadata for a file."""
        
        with self.tracer.start_as_current_span("extract metadata"):
            file_ext = os.path.splitext(file_path)[1].lower()
            metadata = {
                "file_path": file_path,
                "file_type": file_ext,
                "file_name": os.path.basename(file_path),
                "directory": os.path.dirname(file_path),
                "size_bytes": len(content.encode('utf-8')),
                "num_lines": len(content.splitlines()),
                "is_empty": len(content.strip()) == 0,
                "has_shebang": content.startswith('#!') if content else False,
            }
            
            return metadata
            
    def __setup_vector_store(self):
        """Setup vector store collection."""
        with self.tracer.start_as_current_span("setup verctor store"):  
            vector_size = len(self.embeddings.embed_query("test"))      
            self.logger.info(f"collection: {self.collection_name} vector size: {vector_size}")
            if not self.qdrant.collection_exists(self.collection_name):
                self.logger.info(f"Collection {self.collection_name} does not exist. Will create now. Vector size is {vector_size}")
                self.qdrant.create_collection(
                    collection_name=self.collection_name,
                    vectors_config={
                        "page_content_vector": rest.VectorParams(size=vector_size, distance=rest.Distance.COSINE),
                    })
                # self.qdrant.create_collection(
                #     collection_name=self.collection_name,
                #     vectors_config=rest.VectorParams(
                #         size=vector_size,
                #         distance=rest.Distance.COSINE
                #     )
                # )
                self.logger.info(f"Created collection: {self.collection_name}")
            else:
                self.logger.info(f"Collection {self.collection_name} already exists")
 
            self.vector_store = QdrantVectorStore(
                client=self.qdrant,
                collection_name=self.collection_name,
                vector_name="page_content_vector",
                embedding=self.embeddings
            )
            
    def __create_chunking_strategies(self) -> Dict[str, RecursiveCharacterTextSplitter]:
        """Create specialized chunking strategies for different file types."""
        return {
            # Markdown files
            '.md': MarkdownHeaderTextSplitter(
                headers_to_split_on=[
                    ("#", "Header 1"),
                    ("##", "Header 2"),
                    ("###", "Header 3"),
                    ("####", "Header 4")
                ]
            ),
            # Config files
            '.yml': RecursiveCharacterTextSplitter(
                chunk_size=300,
                chunk_overlap=50,
                separators=["---", "\n\n", "\n"]
            ),
            '.yaml': RecursiveCharacterTextSplitter(
                chunk_size=300,
                chunk_overlap=50,
                separators=["---", "\n\n", "\n"]
            ),
            # Default
            'default': RecursiveCharacterTextSplitter(
                chunk_size=400,
                chunk_overlap=50,
                separators=["\n\n", "\n", ". ", " "]
            )
        }