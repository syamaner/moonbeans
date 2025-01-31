from enum import Enum

class ModelProvider(Enum):
    Ollama = 0
    OllamaHost = 1
    OpenAI = 2
    HuggingFace = 3