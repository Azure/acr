# Track base image update from any Azure Container Registry

ACR Tasks supports automated builds for when a container's base iamge is updated. Previously, these automated builds were supported for base images in public repositories, such as DockerHub, or from base images from a task's home registry. Now ACR Tasks can also track base image updates from another ACR than the task's. This base image registry can be **any Azure Container Registry, anywhere in the world**, even geo-replicated. This tutorial combines much of the steps seen in the existing tutorials for [base image update](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-tutorial-base-image-update) and [cross-registry auth](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-tasks-cross-registry-authentication).


# Prerequisites

In this tutorial you will need to first create two registries in different regions.
* The first registry (`homeRegistry`) will be used to create and execute a task. Create this registry in West US.
* The second registry (`baseRegistry`) will host a base image that will be used by the task to build an image. Create this registry in East US.


## Prepare Base Registry

Fork the repo https://github.com/Azure-Samples/acr-build-helloworld-node.git

In the current directory build and push the base image to the base registry

```
az acr build --image baseimages/node:9-alpine --registry baseRegistry --file Dockerfile-base .
```

## Create a task that tracks the private base image

Create a task in the home registry with a system-assigned identity. This task depends on the base image built above.

```
az acr task create \
  --registry homeRegistry \
  --name hometask \
  --image helloworld:v2 \
  --context https://github.com/$GIT_USER/acr-build-helloworld-node.git \
  --file Dockerfile-app \
  --git-access-token $GIT_PAT \
  --arg REGISTRY_NAME=baseRegistry.azurecr.io \
  --assign-identity
```

## Give the identity pull permissions to the base registry

You must grand the managed identity pull permission to the base image registry.
```
principalID=$(az acr task show --name hometask --registry homeRegistry --query identity.principalId --output tsv)
baseregID=$(az acr show --name baseRegistry --query id --output tsv)
```

Only allow the task to pull images from the base image registry.

```
az role assignment create --assignee $principalID --scope $baseregID --role acrpull
```

## Add target registry credentials to the task

Add a custom registry credential to the task so that it can authenticate to the base image registry.
```
az acr task credential add \
  --name hometask \
  --registry homeRegistry \
  --login-server baseRegistry.azurecr.io \
  --use-identity [system]
```

## Manually run the task

You must run a task first in order to track the private base image.
```
az acr task run --registry homeRegistry --name hometask
```

## Update the base image

Here you simulate a framework patch in the base image. Edit Dockerfile-base, and add an "a" after the version number defined in NODE_VERSION:
`ENV NODE_VERSION 9.11.2a`

Now build and push the updated base image to the base registry.
```
az acr build --registry baseRegistry --image baseimages/node:9-alpine --file Dockerfile-base .
```

## List the updated build
```
az acr task list-runs --registry homeRegistry --name hometask -o table
```