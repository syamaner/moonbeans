FROM quay.io/jupyter/pytorch-notebook:latest

USER root
RUN apt-get update && apt-get install -y \
    build-essential \
    curl \
    software-properties-common \
    git \
    && rm -rf /var/lib/apt/lists/*
RUN apt-get update && apt-get install -y libmagic-dev

RUN mkdir /app
COPY requirements.txt /app
RUN pip install -r /app/requirements.txt


USER ${NB_UID}
ENTRYPOINT ["start-notebook.sh"]