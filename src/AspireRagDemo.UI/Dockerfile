FROM python:3.9-slim
ARG PORT=8501
ENV APP_PORT=$PORT
WORKDIR /app


RUN apt-get update && apt-get install -y \
    build-essential \
    curl \
    software-properties-common \
    git \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt /app
RUN pip3 install -r /app/requirements.txt

COPY main.py /app
COPY TraceSetup.py /app
COPY entrypoint.sh /app
RUN chmod +x /app/entrypoint.sh

EXPOSE ${PORT}

RUN groupadd -r -g 65532 ollama && useradd --create-home --shell /bin/bash --uid 65532 -g ollama ui_user

USER 65532:65532

HEALTHCHECK CMD curl --fail http://localhost:${PORT}/_stcore/health

ENTRYPOINT [ "bash", "/app/entrypoint.sh"] 

