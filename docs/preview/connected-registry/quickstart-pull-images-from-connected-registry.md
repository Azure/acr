---
title: Quickstart - Pull images from a connected registry
description: Use Azure Container Registry CLI commands to configure a client token and pull images from a connected registry.
ms.topic: quickstart
ms.date: 12/04/2020
ms.author: memladen
author: toddysm
ms.custom:
---

# Quickstart: Pull images from a connected registry

In this quickstart, you use [Azure Container Registry][container-registry-intro] commands to configure a client token for a connected registry and use this client token to pull images. You can review the [ACR connected registry introduction](intro-connected-registry.md) for details about the connected registry feature of Azure Container Registry.

## Prerequisites

- Use [Azure Cloud Shell](https://docs.microsoft.com/en-us/azure/cloud-shell/quickstart) using the bash environment.
  
  [![https://docs.microsoft.com/en-us/azure/includes/media/cloud-shell-try-it/hdi-launch-cloud-shell.png](https://docs.microsoft.com/en-us/azure/includes/media/cloud-shell-try-it/hdi-launch-cloud-shell.png)](https://shell.azure.com/)
- If you prefer, [install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) the Azure CLI to run CLI reference commands.
  - If you're using a local install, sign in with Azure CLI by using the [az login](https://docs.microsoft.com/en-us/cli/azure/reference-index#az_login) command. To finish the authentication process, follow the steps displayed in your terminal. See [Sign in with Azure CLI](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) for additional sign-in options.
  - When you're prompted, install Azure CLI extensions on first use. For more information about extensions, see [Use extensions with Azure CLI](https://docs.microsoft.com/en-us/cli/azure/azure-cli-extensions-overview).
  - Run [az version](https://docs.microsoft.com/en-us/cli/azure/reference-index?#az_version) to find the version and dependent libraries that are installed. To upgrade to the latest version, run [az upgrade](https://docs.microsoft.com/en-us/cli/azure/reference-index?#az_upgrade).
- The Azure CLI commands in this article are formatted for the Bash shell. If you're using a different shell like PowerShell or Command Prompt, you may need to adjust line continuation characters or variable assignment lines accordingly. This article uses variables to minimize the amount of command editing required.

## Before you begin

Make sure that you have created the connected registry resource in Azure as described in the [Create connected registry using the CLI][quickstart-connected-registry-cli] quickstart guide and have a connected registry deployed on your premises as described in [Quickstart: Deploy a connected registry to an IoT Edge device](quickstart-deploy-connected-registry-iot-edge-cli.md) or [Quickstart: Deploy a connected registry to an Azure Arc cluster](quickstart-deploy-connected-registry-azure-arc.md).

## Create a scope map

Use the following CLI command to create a scope map for read access to the `hello-world` repository:

```azurecli
az acr scope-map create \
  --name hello-world-scopemap \
  --registry mycontainerregistry001 \
  --repository hello-world content/read \
  --description "Scope map for the connected registry."
```

## Create a client token

Use the following CLI command to create a client token and associate it with the newly created scope map:

```azurecli
az acr token create \
  --name myconnectedregistry-client-token \
  --registry mycontainerregistry001 \
  --scope-map hello-world-scopemap
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

## Pull an image from the connected registry

From a machine with access to the connected registry instance, use the following command to sign into the connected registry:

```
docker login -u myconnectedregistry-client-token -p <use_the_password_for_the_token> <use_the_ip_address_of_the_connected_registry>
```

Use the following command to pull the `hello-world` image:

```
docker pull <use_the_ip_address_of_the_connected_registry>/hello-world
```

## Next steps

In this quickstart, you learned how to configure a client token for the connected registry and pull a container image.

<!-- LINKS - internal -->
[az-acr-token-credential-generate]: https://docs.microsoft.com/cli/azure/acr/token/credential?view=azure-cli-latest#az_acr_token_credential_generate
[container-registry-intro]: container-registry-intro.md
[quickstart-connected-registry-cli]: quickstart-connected-registry-cli.md