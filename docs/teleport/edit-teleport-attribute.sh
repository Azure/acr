#!/bin/bash
#usage: edit-teleport-attribute.sh acr-name repo enable
#usage: eg: edit-teleport-attribute.sh demo42 /demo42/hello-world 2.1 disable
#usage: eg: edit-teleport-attribute.sh demo42 /demo42/hello-world 2.1 enable --debug
# Assumes ACR_USER and ACR_PWD are set to valid ACRPULL role
# Retrieve the ACR_PWD with the following command, if the Admin account is enabled
# ACR_PWD="$(az acr credential show -n demo42t --query passwords[0].value -o tsv)"
# NOTE: Repo scoped tokens will be coming online in November
ACR_NAME=$1
ACR_REPO=$2
STATE=$3
DEBUG=$4

# Troubleshooting 
if [[ $DEBUG = '--debug' ]]; then
    echo "Parameter Validation:"
    echo "  ACR_USER: ${ACR_USER}"
    echo "  ACR_PWD : ${ACR_PWD}"
    echo "  ACR_NAME: ${ACR_NAME}"
    echo "  ACR_REPO: ${ACR_REPO}"
fi

echo "Getting Access Token"

ACR_ACCESS_TOKEN=$(curl -s -u ${ACR_USER}:${ACR_PWD} "https://${ACR_NAME}.azurecr.io/oauth2/token?service=${ACR_NAME}.azurecr.io&scope=repository:${ACR_REPO}:pull,push" | sed -e 's/[{}]/''/g' | awk -v RS=',"' -F: '/access_token/ {print $2}' | sed 's/^.//;s/.$//')

if [[ $ACR_ACCESS_TOKEN == '' ]]; then
    echo "Could not get access token, make sure credentials are accurate and have pull access"
    exit 1
fi

if [[ $DEBUG == '--debug' ]]; then
    echo "  ACR_ACCESS_TOKEN: ${ACR_ACCESS_TOKEN}"
fi

SET_STATE=""

if [[ $STATE == 'enable' ]]; then
   SET_STATE="{\"teleportEnabled\": true }"
fi

if [[ $STATE == 'disable' ]]; then
    SET_STATE="{\"teleportEnabled\": false }"
fi

if [[ $DEBUG == '--debug' ]]; then
    echo "  SET_STATE: ${SET_STATE}"
fi

echo "Sendng Patch Request for $ACR_REPO"

RESULT=$(curl -s -S \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${ACR_ACCESS_TOKEN}" \
  --request PATCH \
  --data "${SET_STATE}" \
  https://$ACR_NAME.azurecr.io/acr/v1/$ACR_REPO)

echo "  RESULT: ${RESULT}"


