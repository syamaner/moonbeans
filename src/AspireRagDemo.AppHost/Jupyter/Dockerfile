FROM quay.io/jupyter/minimal-notebook:python-3.12.8
ENV PYTHONDONTWRITEBYTECODE=1

USER root

RUN apt-get update && apt-get install -y libmagic-dev \
    && apt-get install software-properties-common -y

RUN add-apt-repository ppa:dotnet/backports \
    && apt-get update \
    && apt-get install -y dotnet-sdk-9.0 \
    && apt-get clean && rm -rf /var/lib/apt/lists/*
    
USER ${NB_UID}

RUN dotnet tool install -g Microsoft.dotnet-interactive
ENV PATH="${PATH}:/home/jovyan/.dotnet/tools"

COPY ./requirements.txt /home/jovyan/requirements.txt
RUN pip install --no-cache-dir -r /home/jovyan/requirements.txt

# The process for generatin evaluation data requires these data files so we will do that during dokcer build.
RUN python -c "import nltk; nltk.download('punkt_tab'); nltk.download('averaged_perceptron_tagger_eng')"
RUN dotnet interactive jupyter install

ENTRYPOINT ["start-notebook.sh"]