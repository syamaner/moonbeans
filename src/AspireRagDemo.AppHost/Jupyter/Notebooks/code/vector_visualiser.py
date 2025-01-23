import numpy as np
import matplotlib.pyplot as plt
from sklearn.decomposition import PCA
from typing import List, Tuple

class VectorVisualizer:
    def __init__(self, embeddings):
        self.embeddings = embeddings
        self.pca = PCA(n_components=2)
        
    def get_embeddings(self, words: List[str]) -> np.ndarray:
        """Get embeddings for a list of words."""
        return np.array([self.embeddings.embed_query(word) for word in words])
    
    def reduce_dimensions(self, vectors: np.ndarray) -> np.ndarray:
        """Reduce vectors to 2D using PCA."""
        return self.pca.fit_transform(vectors)
    
    def plot_vectors(self, words: List[str], vectors_2d: np.ndarray, 
                    operations: List[Tuple[int, int, str]] = None, 
                    result_vectors: List[Tuple[np.ndarray, str]] = None):
        """
        Plot word vectors and their operations in 2D space.
        
        Args:
            words: List of input words
            vectors_2d: 2D vectors after PCA
            operations: List of tuples (idx1, idx2, operation) for vector operations
            result_vectors: List of tuples (vector, label) for resulting vectors
        """
        plt.figure(figsize=(12, 8))
        
        # Plot original vectors
        plt.scatter(vectors_2d[:, 0], vectors_2d[:, 1], c='blue', alpha=0.5)
        for i, word in enumerate(words):
            plt.annotate(word, (vectors_2d[i, 0], vectors_2d[i, 1]), 
                        xytext=(5, 5), textcoords='offset points')
            plt.arrow(0, 0, vectors_2d[i, 0], vectors_2d[i, 1], 
                     head_width=0.05, head_length=0.1, fc='blue', ec='blue', alpha=0.5)
        
        # Plot operations
        if operations:
            for idx1, idx2, op in operations:
                if op == '+':
                    plt.arrow(vectors_2d[idx1, 0], vectors_2d[idx1, 1],
                            vectors_2d[idx2, 0], vectors_2d[idx2, 1],
                            head_width=0.05, head_length=0.1, fc='green', ec='green',
                            linestyle='--', alpha=0.5)
        
        # Plot result vectors
        if result_vectors:
            for vector, label in result_vectors:
                vector_2d = self.pca.transform([vector])[0]
                plt.arrow(0, 0, vector_2d[0], vector_2d[1],
                         head_width=0.05, head_length=0.1, fc='red', ec='red')
                plt.annotate(label, (vector_2d[0], vector_2d[1]),
                           xytext=(5, 5), textcoords='offset points', color='red')
        
        plt.axhline(y=0, color='k', linestyle='-', alpha=0.3)
        plt.axvline(x=0, color='k', linestyle='-', alpha=0.3)
        plt.grid(True, alpha=0.3)
        plt.title('Word Vectors in 2D Space')
        plt.show()

    def demonstrate_vector_operations(self):
        """Demonstrate various vector operations with word embeddings."""
        # Get embeddings for example words
        words = ['car', 'forest', 'mansion', 'house']
        vectors = self.get_embeddings(words)
        vectors_2d = self.reduce_dimensions(vectors)
        
        # # Example 1: Basic vectors
        self.plot_vectors(words, vectors_2d)
        
        # # Example 2: Vector addition (cat + house - hotel ≈ dog house)
        # result_vector = vectors[0] + vectors[3] - vectors[2]  # cat + house - hotel
        # self.plot_vectors(
        #     words, vectors_2d,
        #     operations=[(0, 3, '+'), (3, 2, '-')],
        #     result_vectors=[(result_vector, 'cat_house')]
        # )
        
        # # Example 3: Analogy (dog is to house as cat is to ?)
        # analogy_vector = vectors[1] + vectors[3] - vectors[0]  # dog + house - cat
        # self.plot_vectors(
        #     words, vectors_2d,
        #     operations=[(1, 3, '+'), (3, 0, '-')],
        #     result_vectors=[(analogy_vector, 'dog_house')]
        # )

