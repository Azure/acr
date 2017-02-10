#!/bin/bash
set -e

CERT_FINGERPRINT=$1
export BACKEND_HOST=$2
export FRONTEND_HOST=$3
CA_CERT_URL=$4

SOURCE_ROOT="https://raw.githubusercontent.com/shhsu/acr/master"

curl "$SOURCE_ROOT/docs/custom-domain/docker-vm-deploy/setup-certs.sh" -o  setup-certs.sh
chmod +x ./setup-certs.sh
. ./setup-certs.sh $CERT_FINGERPRINT $CA_CERT_URL

export CONTAINER_CERT_LOCATION="/etc/nginx/ssl/cert.crt"
export CONTAINER_PRV_LOCATION="/etc/nginx/ssl/private.key"

curl "$SOURCE_ROOT/docs/custom-domain/docker-vm-deploy/docker-compose.yml.template" -o  docker-compose.yml.template
sudo -E envsubst '$CERT_LOCATION$PRV_LOCATION$CONTAINER_CERT_LOCATION$CONTAINER_PRV_LOCATION' < docker-compose.yml.template > docker-compose.yml

export CERT_LOCATION=$CONTAINER_CERT_LOCATION
export PRV_LOCATION=$CONTAINER_PRV_LOCATION

curl "$SOURCE_ROOT/docs/custom-domain/docker-vm-deploy/nginx.conf.template" -o  nginx.conf.template
sudo -E envsubst '$FRONTEND_HOST$BACKEND_HOST$CERT_LOCATION$PRV_LOCATION' < nginx.conf.template > nginx.conf

## Docker installation extension installs docker in the background
## So we cannot make assumption about its completion time
until docker-compose up
do
    sleep 10
done
