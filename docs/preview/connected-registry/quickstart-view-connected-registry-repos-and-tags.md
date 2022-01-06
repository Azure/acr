---
title: Quickstart - View connected registry repositories and tags
description: Use curl commands to view the repositories and tags stored in a deployed connected registry.
ms.topic: quickstart
ms.date: 01/06/2022
ms.author: jeburke
author: jaysterp
ms.custom:
---

# Quickstart: View repositories and tags in a deployed connected registry

In this quickstart, you use [Azure Container Registry][container-registry-intro] and [curl](https://curl.se/) commands to view available repositories and tags in a deployed connected registry. You can review the [ACR connected registry introduction](intro-connected-registry.md) for details about the connected registry feature of Azure Container Registry.

## Prerequisites

- Use [Azure Cloud Shell](https://docs.microsoft.com/en-us/azure/cloud-shell/quickstart) using the bash environment.
  
  [![https://docs.microsoft.com/en-us/azure/includes/media/cloud-shell-try-it/hdi-launch-cloud-shell.png](https://docs.microsoft.com/en-us/azure/includes/media/cloud-shell-try-it/hdi-launch-cloud-shell.png)](https://shell.azure.com/)
- If you prefer, [install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) the Azure CLI to run CLI reference commands.
  - If you're using a local install, sign in with Azure CLI by using the [az login](https://docs.microsoft.com/en-us/cli/azure/reference-index#az_login) command. To finish the authentication process, follow the steps displayed in your terminal. See [Sign in with Azure CLI](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) for additional sign-in options.
  - When you're prompted, install Azure CLI extensions on first use. For more information about extensions, see [Use extensions with Azure CLI](https://docs.microsoft.com/en-us/cli/azure/azure-cli-extensions-overview).
  - Run [az version](https://docs.microsoft.com/en-us/cli/azure/reference-index?#az_version) to find the version and dependent libraries that are installed. To upgrade to the latest version, run [az upgrade](https://docs.microsoft.com/en-us/cli/azure/reference-index?#az_upgrade).
- The Azure CLI commands in this article are formatted for the Bash shell. If you're using a different shell like PowerShell or Command Prompt, you may need to adjust line continuation characters or variable assignment lines accordingly. This article uses variables to minimize the amount of command editing required.

## Before you begin

Make sure that you have created the connected registry resource in Azure as described in the [Create connected registry using the CLI][quickstart-connected-registry-cli] quickstart guide and have a connected registry deployed on your premises as described in [Quickstart: Deploy a connected registry to an IoT Edge device](quickstart-deploy-connected-registry-iot-edge-cli.md) or [Quickstart: Deploy a connected registry to Kubernetes cluster](quickstart-deploy-connected-registry-kubernetes.md). 

In this tutorial, we configure a _client token_ to list available repositories and tags. Reference [Understand access to a connected registry](overview-connected-registry-access.md) for more information on how client tokens are used for authentication with a connected registry.


## Create a client token

Use the following CLI command to create a token with read access to your synced repositories. We need both `content/read` and `metadata/read` permissions to list the repositories and tags in the connected registry. The below command creates a token with read access to the "hello-world" and "testing" repositories. 

```azurecli
az acr token create \
  --name myconnectedregistry-client-token \
  --registry mycontainerregistry001 \
  --repository hello-world content/read metadata/read \
  --repository testing content/read metadata/read
```

The command will return details about the newly generated token including passwords.

  > [!IMPORTANT]
  > Make sure that you save the generated passwords. Those are one-time passwords and cannot be retrieved. You can generate new passwords using the [az acr token credential generate][az-acr-token-credential-generate] command.

## Update the connected registry with the client token

Use the following CLI command to update the connected registry with the newly created client token:

```azurecli
az acr connected-registry update \
  --name myconnectedregistry \
  --registry mycontainerregistry001 \
  --add-client-token myconnectedregistry-client-token
```

## View the repositories in a deployed connected registry

Run the following commands from a machine with access to the deployed connected registry.

1. Install [jq](https://stedolan.github.io/jq/), a command-line JSON processor. This utility is used to parse JSON, and is useful in parsing the response when listing repositories and tags of a connected registry. From the open SSH connection, enter following command to install jq:

```
sudo apt -y install jq
```

2. Run the following command to base64 encode your client token credentials and store this value in a variable.

```
ENCODED_CREDENTIALS=$(echo -n 'myconnectedregistry-client-token:<insert client token password>' | base64)
```

3. The following command can be referenced to acquire a token to list repositories. 

```
curl --location  \
      --request GET 'https://<IP_address_or_FQDN_of_connected_registry>:<port>/acr/oauth2/token?service=<IP_address_or_FQDN_of_connected_registry>:<port>&scope=registry:catalog:*'  \
      --header 'Authorization: Basic <base64 encode(client token username:password)>'
```

In this example, the connected registry endpoint is accessible over HTTP on localhost:8080. The following command fetches the access token from the connected registry and stores it in a variable.

```
ACCESS_TOKEN=$(curl --location  \
  --request GET 'http://localhost:8080/acr/oauth2/token?service=localhost:8080&scope=registry:catalog:*'  \
  --header 'Authorization: Basic '$ENCODED_CREDENTIALS | jq -r '.access_token')
```

4. Using the above token, run the following command to list the repositories available on the connected registry.

```
curl --location \
 --request GET 'http://localhost:8080/v2/_catalog' \
 --header 'Authorization: Bearer '$ACCESS_TOKEN | jq '.'
```

You will see an output similar to:

```json
{
  "repositories": [
    "hello-world",
    "testing"
  ]
}
```

> [!IMPORTANT]
> Only those repositories that the client token has access to will be listed if they are available on the connected registry. To give the client token read access to additional repositories, use the [az acr token update][az-acr-token-update] command.

## View the tags for a repository in a deployed connected registry

Run the following commands from a machine with access to the deployed connected registry.

1. Install [jq](https://stedolan.github.io/jq/), a command-line JSON processor. This utility is used to parse JSON, and is useful in parsing the response when listing repositories and tags of a connected registry. From the open SSH connection, enter following command to install jq:

```
sudo apt -y install jq
```

2. Run the following command to base64 encode your client token credentials and store this value in a variable.

```
ENCODED_CREDENTIALS=$(echo -n 'myconnectedregistry-client-token:<insert client token password>' | base64)
```

3. The following command can be referenced to acquire a token to view tags fpr a repository. 

```
 curl --location  \
      --request GET 'https://<IP_address_or_FQDN_of_connected_registry>:<port>/acr/oauth2/token?service=<IP_address_or_FQDN_of_connected_registry>:<port>&scope=repository:<repository name>:pull'  \
      --header 'Authorization: Basic <base64 encode(client token username:password)>'
```

In this example, we will fetch tags for the "hello-world" repository. The connected registry endpoint is accessible over HTTP on localhost:8080. The following command fetches the access token from the connected registry and stores it in a variable.

```
ACCESS_TOKEN=$(curl --location  \
      --request GET 'http://localhost:8080/acr/oauth2/token?service=localhost:8080&scope=repository:hello-world:pull'  \
      --header 'Authorization: Basic '$ENCODED_CREDENTIALS | jq -r '.access_token')
```

4. Using the above token, run the following command to list the tags in the hello-world repositories on the connected registry.

```
curl --location \
   --request GET 'http://localhost:8080/v2/hello-world/tags/list' \
   --header 'Authorization: Bearer '$ACCESS_TOKEN | jq '.'
```

You will see an output similar to:

```json
{
  "name": "hello-world",
  "tags": [
    "0.1.0",
    "0.2.0",
    "0.3.0"
  ]
}
```

<!-- LINKS - internal -->
[az-acr-token-update]: https://docs.microsoft.com/cli/azure/acr/token?view=azure-cli-latest#az_acr_token_update
[container-registry-intro]: https://docs.microsoft.com/azure/container-registry/
[quickstart-connected-registry-cli]: quickstart-connected-registry-cli.md