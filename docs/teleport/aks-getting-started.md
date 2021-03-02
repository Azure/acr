---
title: Integrate Azure Container Registry and Project Teleport with Azure Kubernetes Service
description: Learn how to integrate Azure Kubernetes Service (AKS) with Azure Container Registry (ACR) and Project Teleport
services: container-service
ms.topic: article
ms.date: 02/22/2021
---

# Integrate Azure Container Registry and Project Teleport with Azure Kubernetes Service (preview)

[Project Teleport][project-teleport] allows container hosts to access pre-expanded layers within an [Azure Container Registry (ACR)][acr] that is in the same region as the container host. Using pre-expanded layers removes the time for compute and memory to decompress layers that are already available within the Azure network. Removing this decompression also reduces the time to create the instance of the running container.

Using Project Teleport with ACR and AKS is in preview.

> AKS preview features are available on a self-service, opt-in basis. Previews are provided "as is" and "as available," and they're excluded from the service-level agreements and limited warranty. AKS previews are partially covered by customer support on a best-effort basis. As such, these features aren't meant for production use. AKS preview features aren't available in Azure Government or Azure China 21Vianet clouds. For more information, see the following support articles:
>
> - [AKS support policies][aks-support-policies]
> - [Azure support FAQ][aks-support-faq]
>
> ACR Preview features are available on a self-service, opt-in basis. Previews are provided "as is" and "as available," and they're excluded from the service-level agreements and limited warranty. ACR previews are partially covered by customer support on a best-effort basis. As such, these features aren't meant for production use. ACR preview features aren't available in Azure Government or Azure China 21Vianet clouds.
> - Project Teleport Support is provided through a [Microsoft Teams channel][acr-teleport-red-shirts]. [Requesting access to Project Teleport][teleport-signup-form]

## Prerequisites

* Ensure you have the Azure CLI, version 2.13.0 or greater installed.  
* Ensure you have the `aks-preview` CLI extension 0.4.73 or greater installed.
* Ensure you have Project Teleport enabled on your ACR.
* Ensure you have the AKS `EnableACRTeleport` feature flag under `Microsoft.ContainerService` enabled.

## Limitations

* AKS node pools must use Kubernetes 1.19.7 or greater.
* AKS node pools must use `containerd` as the container runtime. AKS clusters with node pools using Kubernetes before 1.19.0 use Moby as the container runtime.
* Each ACR used must have Project Teleport enabled.
* Each ACR must use the [*Premium* Tier][acr-tiers].
* ACR and AKS must be in the same region. See [Project Teleport supported regions][teleport-regions].  
This is less of a limitation, rather a design constraint. It's always a best practice to have the content required for deployment to be within the same region. Project Teleport depends on this best practice to mount layers within an Azure network regional boundary.
* At this time, Project Teleport supports Linux containers on AKS clusters. Windows support is not yet available.
* At this time, enabling Teleport on an existing registry will not convert images already in the registry. To expand existing content, pull and push the image to trigger expansion.  
_In a future release, enabling Project Teleport on a repository will convert the images. This work is not yet complete._
* ACR Geo-replicated registries are not currently supported on Project Teleport enabled registries.
* [Private Links](https://aka.ms/acr/privatelink) are not currently supported on Project Teleport enabled registries.

### Enable Project Teleport on an ACR

Create a [premium instance][acr-tiers] of Azure Container Registry in one of the [Project Teleport supported regions][teleport-regions].

Sign up for ACR Project Teleport using [the signup form][teleport-signup-form], providing the resource ID of a **Premium Tier** ACR instance. Once Project Teleport is enabled, you'll receive an confirmation email.

#### Confirming an ACR repository is set to expand

Once you receive a confirmation email, push an image to a new repository. You may also use `az acr import` to copy an image from another registry.

To confirm an ACR repository has been configured for teleport expansion, use the following command, replacing `$ACR` and `<namespace/image>` with your acr and image name.

```azurecli
az acr import \
  --source mcr.microsoft.com/azuredocs/azure-vote-front:v1 \
  --name $ACR \
  --image azure-vote-front:v1

az acr import \
  --source mcr.microsoft.com/oss/bitnami/redis:6.0.8 \
  --name $ACR \
  --image redis:6.0.8

az acr repository show -n ${ACR} -o jsonc \
  --repository <namespace/image> 
```

The following example output shows *"teleportEnabled": true*, verifying Project Teleport is enabled on your ACR.

```console
{
  "changeableAttributes": {
    "deleteEnabled": true,
    "listEnabled": true,
    "readEnabled": true,
    "teleportEnabled": true,
    "writeEnabled": true
  },
  ...
}
```

#### Confirming an image has been expanded

At this point in the Teleport preview, check image expansion using the [check-expansion.sh](./check-expansion.sh) script. As the script uses a `/mount` api, basic auth is required. An [ACR Token](https://aka.ms/acr/tokens) is created and saved as environment variable.

```azurecli-interactive
export ACR_USER=teleport-token
export ACR_PWD=$(az acr token create \
  --name teleport-token \
  --registry $ACR \
  --scope-map _repositories_pull \
  --query credentials.passwords[0].value -o tsv)

./check-expansion.sh ${ACR} <namespace/repo> <tag>
# example: ./check-expansion.sh myacr azure-vote-front v1
```

### Install aks-preview CLI extension

To use Project Teleport with ACR and AKS, you need version 0.4.73, or greater, of the *aks-preview* CLI extension. Install the *aks-preview* Azure CLI extension using the [az extension add][az-extension-add] command, or install any available updates using the [az extension update][az-extension-update] command:

```azurecli-interactive
# Check the current Azure CLI version
az version

# Check the current aks-preview extension version
az extension list

# Install the aks-preview extension
az extension add --name aks-preview

# Update the extension to make sure you have the latest version installed
az extension update --name aks-preview
```

### Register the AKS `EnableACRTeleport` preview feature

To use Teleport with ACR and AKS, you must enable the `EnableACRTeleport` feature flag on your subscription. This feature flag provisions the teleportd client to AKS nodes.

Register the AKS `EnableACRTeleport` feature flag using the [az feature register][az-feature-register] command as shown in the following example:

```azurecli-interactive
az feature register \
  --namespace "Microsoft.ContainerService" \
  --name "EnableACRTeleport"
```

It may take 15 minutes or more to complete registration. You can check on the registration status using the [az feature list][az-feature-list] command:

```azurecli-interactive
az feature list \
  --query "[?contains(name, 'Microsoft.ContainerService/EnableACRTeleport')].{Name:name,State:properties.state}" \
  -o table
```

When ready, refresh the registration of the *Microsoft.ContainerService* resource provider using the [az provider register][az-provider-register] command:

```azurecli-interactive
az provider register --namespace Microsoft.ContainerService
```

## Create a new AKS cluster with Teleport enabled

To use Teleport with ACR and AKS on a new cluster, create a new AKS cluster and specify your ACR with Project Teleport enabled as well as the `EnableACRTeleport=true` custom header. Set environment variables to create an AKS cluster attached to an ACR instance. 

```azurecli
AKS=myaks
AKS_RG=${AKS}-rg
LOCATION=westus2
K8S_VERSION=1.19.7
ACR=myacr
ACR_URL=${ACR}.azurecr.io
```

Project Teleport _does not yet_ support managed identity access to teleport expanded layers. Until managed identities are supported, configure the cluster with a service principal.

```azurecli-interactive
# Create a Service Principal for authentication to Teleport expanded layers

AKS_SP_PWD=$(az ad sp create-for-rbac --skip-assignment --name ${AKS}-sp --query password -o tsv)
AKS_SP_ID=$(az ad sp show --id http://${AKS}-sp --query appId -o tsv)

# Create the AKS Cluster, attached to ACR, with Project Teleport Enabled
az aks create \
    -g ${AKS_RG} \
    -n ${AKS} \
    --attach-acr $ACR \
    --kubernetes-version ${K8S_VERSION} \
    -l $LOCATION \
    --client-secret $AKS_SP_PWD \
    --service-principal ${AKS_SP_ID} \
    --aks-custom-headers EnableACRTeleport=true

az aks get-credentials \
    -g ${AKS_RG} \
    -n ${AKS}
```

## Add a Project Teleport enabled node pool to an existing AKS cluster

To use Project Teleport on an existing AKS cluster, add a node pool to your cluster and set the `EnableACRTeleport=true` custom header.

```azurecli
az aks nodepool add \
  --name teleportPool \
  --cluster-name ${AKS} \
  --resource-group ${AKS_RG} \
  --kubernetes-version ${K8S_VERSION} \
  --aks-custom-headers EnableACRTeleport=true
```

If your AKS cluster doesn't have access to your Project Teleport enabled ACR, attach it.

```azurecli
az aks update \
  -n ${AKS} \
  -g ${AKS_RG} \
  --attach-acr ${ACR}
```

## Verify your cluster has Project Teleport enabled

When your cluster has a node pool with Project Teleport enabled, any nodes in that node pool have the `kubernetes.azure.com/enable-acr-teleport-plugin:true` label. You can target this label with a node selector when running pods on your cluster to have specific applications take advantage of Project Teleport.

To show the labels on your nodes, get the credentials for your cluster and use `kubectl` to show your nodes.

```azurecli
az aks get-credentials g ${AKS_RG} -n ${AKS}
kubectl get nodes
```

Use `kubectl` to get the details of a specific node. Confirm the `kubernetes.azure.com/enable-acr-teleport-plugin:true` label appears in the node details. Note: the full name of the nodepool is not required. Only enough of the name to be unique is required.

```azurecli
kubectl describe node aks-nodepool1
```

The following example output shows the `kubernetes.azure.com/enable-acr-teleport-plugin:true` label in the node details.

```console
$ kubectl describe node aks-nodepool1-00000000-vmss000000
...
Name:               aks-nodepool1-00000000-vmss000000
Roles:              agent
Labels:             agentpool=nodepool1
                    ...
                    kubernetes.azure.com/enable-acr-teleport-plugin=true
                    ...
```

## Next steps

- [Deploy two nodes, one with Project Teleport, one without to see the start time differences.](./aks-teleport-comparison.md)

For more information about pushing an image into your ACR, see [Push your first image to a private container registry using the Docker CLI][acr-push].

For more information about importing images into your ACR, see [Import container images to a container registry][acr-import].

[acr]:                     https://aka.ms/acr
[acr-import]:              ../container-registry/container-registry-import-images.md
[acr-teleport-red-shirts]: https://aka.ms/acr/teleport/red-shirts
[acr-tiers]:               https://aka.ms/acr/tiers
[acr-push]:                ../container-registry/container-registry-get-started-docker-cli.md
[az-extension-add]:        /cli/azure/extension#az-extension-add
[az-extension-update]:     /cli/azure/extension#az-extension-update
[az-feature-list]:         /cli/azure/feature#az-feature-list
[az-feature-register]:     /cli/azure/feature#az-feature-register
[az-provider-register]:    /cli/azure/provider#az-provider-register
[teleport-signup-form]:    https://aka.ms/acr/teleport/signup
[project-teleport]:        https://github.com/azurecr/teleport
[teleport-regions]:        ./aks-teleport-comparison.md#preview-constraints
[aks-support-policies]:    https://docs.microsoft.com/azure/aks/support-policies
[aks-support-faq]:         https://docs.microsoft.com/en-us/azure/aks/faq