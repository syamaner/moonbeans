#!/bin/bash

build_number=0.5.7
docker buildx build --push --platform linux/arm64,linux/amd64 --tag syamaner/ollama-nonroot:${build_number}   .