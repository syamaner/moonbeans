FROM quay.io/jupyter/minimal-notebook:python-3.12.8

USER root
RUN apt-get update && apt-get install -y libmagic-dev

RUN mkdir /app
COPY requirements.txt /app

RUN pip install -r /app/requirements.txt

USER ${NB_UID}
ENTRYPOINT ["start-notebook.sh"]