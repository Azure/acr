---
title: Troubleshoot issues with connected registry
description: Symptoms, causes, and resolution of common problems when setting up, configuring, and deploying connected registries
ms.topic: article
ms.date: 01/27/2021
ms.author: memladen
author: toddysm
---

# Troubleshoot issues with connected registry

This article helps you troubleshoot problems you might encounter when setting up, configuring, and deploying a connected registry.

## Symptoms

* Unable to activate the connected registry. The connected registry container fails to start up due to the error `"Failed to activate the connected registry as it is already activated by another instance. Only one instance is supported at any time."`

## Causes

* There is already another instance of this connected registry deployed. There can only be one instance of a connected registry deployed at once.

## Potential solutions

### Deactivate the existing connected registry instance

 If you would like to deactivate the existing instance of the connected registry, run the `az acr connected-registry deactivate` command using the [Azure CLI](https://learn.microsoft.com/cli/azure/acr/connected-registry?view=azure-cli-latest#az-acr-connected-registry-deactivate). Then redeploy the connected registry.

### Create a new connected registry with a different name

If you would like to preserve the existing instance of this connected registry, create a new connected registry with a different name to avoid the activation conflict. Run the `az acr connected-registry create` command using the [Azure CLI](https://learn.microsoft.com/cli/azure/acr/connected-registry?view=azure-cli-latest#az-acr-connected-registry-create) and deploy again using this new connected registry.

## Symptoms

* Unable to push or pull images to or from the connected registry. Client error is `Error response from daemon: Get https://<connected-registry-login-server-ip-or-dns>/v2/: http: server gave HTTP response to HTTPS client`

## Causes

* The connected registry is configured for HTTP access only - [solution](#configure-docker-daemon-to-access-insecure-registry)

## Potential solutions

### Configure Docker daemon to access insecure registry

The access the connected registry via HTTP, you must configure the client Docker daemon to allow access to insecure registries. The steps are described in [Test an insecure registry](https://docs.docker.com/registry/insecure/) article on Docker's web site.

## Symptoms

* Unable to pull an image from the connected registry. Client error is
`Error response from daemon: manifest for <connected-registry-login-server-ip-or-dns>/<repository-name>:<tag> not found: manifest unknown: manifest unknown`

## Causes

* The connected registry is not configured to sync this repository from the Azure Container Registry.

## Potential solutions

### Configure the connected registry to sync the repository

In order to access this image, you must update the connected registry configuration to sync the repository. From the Azure CLI run
`az acr connected-registry repo -r <ACR-name> -n <connected-registry-name> --add <repository-name>`

Wait a few minutes for the connected registry to sync the repository and try pulling the image again.

## Symptoms

* Unable to push or pull images to or from the connected registry. Client error is
`Error response from daemon: pull access denied for <connected-registry-login-server-ip-or-dns>/<repository-name>, repository does not exist or may require 'docker login': denied: Insufficient scopes to perform the operation`

## Causes

* The repository is synced to the connected registry, but the client token used for `docker login` does not have access.

## Potential solutions

### Assign permissions to the connected registry client token

To update the permissions of the client token, you must update the corresponding scope map. To view the scope map resource ID associated with a token, run the following from the Azure CLI:

`az acr token show -r <ACR-name> -n <client-token-name> -o tsv --query scopeMapId`

#### Pull permissions

To give the client token pull permissions to the repository, run the following from the Azure CLI:

```
az acr scope-map update \
  --name <scope-map-name> \
  --registry <ACR-name> \
  --add-repository <repository-name> content/read
```

#### Push permissions

If the connected registry is in Registry mode, the client may need push access. To give the client token push permissions to the repository, run the following from the Azure CLI:

```
az acr scope-map update \
  --name <scope-map-name> \
  --registry <ACR-name> \
  --add-repository <repository-name> content/read content/write
```

Wait a few minutes for the updated client token permissions to sync to the connected registry.

  > [!TIP]
  > After updating the permissions of the client token, you may want to generate new passwords. Run `az acr token credential generate` from the Azure CLI to refresh your client token passwords. Allow a few minutes for the credentials to sync to the connected registry. Login using your new credentials with `docker login`.

For more information on ACR token management please reference [Create a token with repository-scoped permissions](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-repository-scoped-permissions]).

## Symptoms

* Unable to push an image to the connected registry. Client error is
`denied: This operation is not allowed on this registry.`

## Causes

* Images can only be pushed to a connected registry in `Registry` mode. If the connected registry is in `Mirror` mode then only readonly operations are allowed, such as `docker pull`.

## Potential solutions

### Create a new connected registry in Registry mode

Once a connected registry is created, the mode cannot be changed. If you would like to push images to your connected registry, create a new resource in Registry mode. Ensure the client token linked to the new connected registry has push permissions to the synced repositories.

From the Azure CLI run 

`az acr connected-registry create --registry <ACR-name> --name <connected-registry-name> --repository app/hello-world service/mycomponent --client-tokens <client-token-name>`

Deploy the connected registry. For an example on how to deploy a connected registry on IoT Edge, please reference [Quickstart - Deploy a connected registry to an IoT Edge device](./quickstart-deploy-connected-registry-iot-edge-cli.md). Once deployed, you can use the client token to login and push images to the connected registry. These images will be synced from the connected registry to the ACR.
