#!/bin/bash
#usage: check-expansion.sh acr-name repo tag
#usage: eg: check-expansion.sh demo42 /demo42/hello-world 2.1
#usage: eg: check-expansion.sh demo42 /demo42/hello-world 2.1 --debug
# Assumes ACR_USER and ACR_PWD are set to valid ACRPULL role
# Retrieve the ACR_PWD with the following command, if the Admin account is enabled
# ACR_PWD="$(az acr credential show -n demo42t --query passwords[0].value -o tsv)"
# NOTE: Repo scoped tokens will be coming online in November
ACR_NAME=$1
ACR_REPO=$2
ACR_TAG=$3
DEBUG=$4

# Troubleshooting 
if [ $DEBUG = '--debug' ]; then
    echo "Parameter Validation:"
    echo "  ACR_USER: ${ACR_USER}"
    echo "  ACR_PWD : ${ACR_PWD}"
    echo "  ACR_NAME: ${ACR_NAME}"
    echo "  ACR_REPO: ${ACR_REPO}"
    echo "  ACR_TAG : ${ACR_TAG}"
fi

echo "Getting Access Token"

ACR_ACCESS_TOKEN=$(curl -s -u ${ACR_USER}:${ACR_PWD} "https://${ACR_NAME}.azurecr.io/oauth2/token?service=${ACR_NAME}.azurecr.io&scope=repository:${ACR_REPO}:pull" | sed -e 's/[{}]/''/g' | awk -v RS=',"' -F: '/access_token/ {print $2}' | sed 's/^.//;s/.$//')

if [[ $ACR_ACCESS_TOKEN == '' ]]; then
    echo "Could not get access token, make sure credentials are accurate and have pull access"
    exit 1
fi

if [[ $DEBUG == '--debug' ]]; then
    echo "  ACR_ACCESS_TOKEN: ${ACR_ACCESS_TOKEN}"
fi

echo "Finding Digest for $ACR_REPO:$ACR_TAG https://$ACR_NAME.azurecr.io/v2/$ACR_REPO/manifests/$ACR_TAG"

ACR_DIGEST=$(curl -s -L -I \
  -H "Accept: application/vnd.docker.distribution.manifest.v2+json" \
  -H "Authorization: Bearer ${ACR_ACCESS_TOKEN}" \
  https://$ACR_NAME.azurecr.io/v2/$ACR_REPO/manifests/$ACR_TAG | grep ^Docker-Content-Digest | awk '{print $2}' | head -c 71)

if [[ $DEBUG == '--debug' ]]; then
    echo "  ACR_DIGEST: ${ACR_DIGEST}"
fi

if [[ $ACR_DIGEST == '' ]]; then
    echo "Could not get image digest, make sure this tag exists"
    exit 1
fi

echo "Checking https://$ACR_NAME.azurecr.io/mount/v1/$ACR_REPO/_manifests/$ACR_DIGEST"
while true
do
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "Authorization: Bearer ${ACR_ACCESS_TOKEN}" \
    "https://$ACR_NAME.azurecr.io/mount/v1/$ACR_REPO/_manifests/$ACR_DIGEST")

    echo "Status: ${STATUS}"
    if [ $STATUS -eq 200 ]; then
        echo "Teleport: layers ready"
        break
    elif [ $STATUS -eq 409 ]; then
        echo "Teleport: expanding layers"
    elif  [ $STATUS -eq 404 ]; then
        echo "Teleport: ${ACR_NAME}-${ACR_REPO}:${ACR_TAG} not enabled"
        break
    else
        echo "Unknown status $STATUS"
    fi
    sleep 2
done
