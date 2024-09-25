---
title: Quickstart - Deploy a connected registry to Kubernetes cluster - V2
description: Use Azure Container Registry CLI and Helm 3 commands to deploy a connected registry to a Kubernetes cluster.
ms.topic: quickstart
ms.date: 09/19/2024
ms.author: yuanxi
author: xyxyxyxyxyxy
ms.custom:
---

# Quickstart:  Deploy a connected registry to Kubernetes cluster

In this quickstart, you use [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-intro) and [Helm 3](https://helm.sh/docs/intro/quickstart/) commands to deploy a connected registry Helm chart to a Kubernetes cluster. You can review the [ACR connected registry introduction](https://docs.microsoft.com/en-us/azure/container-registry/intro-connected-registry) for details about the connected registry feature of Azure Container Registry. For more details on Helm charts, see [Helm documentation](https://helm.sh/docs/topics/charts/).

## Prerequisites

- [Install or upgrade Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) to version >= 2.64.0
  - If you're using a local install, sign in with Azure CLI by using the [az login](https://docs.microsoft.com/en-us/cli/azure/reference-index#az_login) command. To finish the authentication process, follow the steps displayed in your terminal. See [Sign in with Azure CLI](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) for additional sign-in options.
- The Azure CLI commands in this article are formatted for the Bash shell. If you're using a different shell like PowerShell or Command Prompt, you may need to adjust line continuation characters or variable assignment lines accordingly. This article uses variables to minimize the amount of command editing required.
- Both `ReadWrite` and `ReadOnly` modes will work for this scenario.
- An up-and-running Kubernetes cluster. If you don't have one, you can create a cluster using one of these options:
    - [Kubernetes in Docker](https://kind.sigs.k8s.io/)
    - [K3s: Lightweight Kubernetes](https://rancher.com/docs/k3s/latest/quick-start/) cluster.
    - Self-managed Kubernetes cluster using [Cluster API](https://cluster-api.sigs.k8s.io/user/quick-start.html)
    - An [Azure Kubernetes Service](https://docs.microsoft.com/azure/aks/kubernetes-walkthrough) cluster
- [Helm 3](https://helm.sh/docs/intro/install/) installed. **Note: the following tutorial is compatible for Helm releases >= v3.10.0.**
- [Kubectl](https://kubernetes.io/docs/tasks/tools/#kubectl) installed.
- A `kubeconfig` file and context pointing to your cluster.

## Create the Connected registry and synchronize with ACR

Creating the Connected registry to synchronize with ACR is the foundation step for deploying the Connected registry Arc extension.

Create the Connected registry, which synchronizes with the ACR registry:

To create a Connected registry myconnectedregistry that synchronizes with the ACR registry myacrregistry in the resource group myresourcegroup and the repository hello-world, you can run the [az acr connected-registry create](https://review.learn.microsoft.com/en-us/cli/azure/acr/connected-registry?view=azure-cli-latest&branch=main#az-acr-connected-registry-create) command:

```cli
az acr connected-registry create --registry myacrregistry \ 
  --name myconnectedregistry \
  --resource-group myresourcegroup \ 
  --repository "hello-world"
```

- The [az acr connected-registry create](https://review.learn.microsoft.com/en-us/cli/azure/acr/connected-registry?view=azure-cli-latest&branch=main#az-acr-connected-registry-create) command creates the Connected registry with the specified repository.

- The [az acr connected-registry create](https://review.learn.microsoft.com/en-us/cli/azure/acr/connected-registry?view=azure-cli-latest&branch=main#az-acr-connected-registry-create) command overwrites actions if the sync scope map named myconnectedregistry exists and overwrites properties if the sync token named myconnectedregistry exists.

- The [az acr connected-registry create](https://review.learn.microsoft.com/en-us/cli/azure/acr/connected-registry?view=azure-cli-latest&branch=main#az-acr-connected-registry-create) command validates a dedicated data endpoint during the creation of the Connected registry and provides a command to enable the dedicated data endpoint on the ACR registry.

## Install the Connected Registry Helm Chart

From a Kubernetes cluster node, run the following commands to install the connected registry helm chart.

1. Set environment variable to enable OCI artifact support in the Helm 3 client.

`export HELM_EXPERIMENTAL_OCI=1`

2. Get the connection string for your connected registry. This command generates `password1` of the corresponding sync token resource.

```cli
az acr connected-registry get-settings \
  --registry $REGISTRY_NAME \
  --name $CONNECTED_REGISTRY_RW \
  --generate-password 1 \
  --parent-protocol https
```

Command output includes the registry connection string and related settings. The following example output shows the connection string for the connected registry named _myconnectedregistry_ with parent registry _contosoregistry_.

```json
{
  "ACR_REGISTRY_CONNECTION_STRING": "ConnectedRegistryName=myconnectedregistry;SyncTokenName=myconnectedregistry-sync-token;SyncTokenPassword=xxxxxxxxxxxxxxxx;ParentGatewayEndpoint=contosoregistry.eastus.data.azurecr.io;ParentEndpointProtocol=https"
}
```

3. Decide what storage class resource is appropriate for your Kubernetes distribution. The user is required to provide an existing storage class resource name when deploying the connected registry. You can research your distribution to learn more on predefined storage classes or how to create your own. For instance, see predefined storage resources on AKS at [Concepts - Storage in Azure Kubernetes Services (AKS)](https://docs.microsoft.com/en-us/azure/aks/concepts-storage#storage-classes). To view storage class resources on your cluster, run

`kubectl get sc`

4. Run the following command to install the connected registry helm chart

`helm install --namespace connected-registry connected-registry oci://mcr.microsoft.com/acr/connected-registry/chart --create-namespace --set connectionString="<insert connection string>" --set service.clusterIP="<insert service ip>" --set pvc.storageClassName=<insert storage class name>`

5. To view the deployed connected registry resources, run 

`kubectl get services,deployments,pods,secrets -n connected-registry`

Note: you will see the connected registry service resource running under the cluster IP you selected.


## Pull from the Connected Registry

For more information, reference [Pull images from a connected registry](https://docs.microsoft.com/en-us/azure/container-registry/pull-images-from-connected-registry).

1. Get credentials corresponding to a client token linked to the connected registry. For more information, see [Manage client tokens](https://docs.microsoft.com/en-us/azure/container-registry/overview-connected-registry-access#manage-client-tokens). The following example generates `password1` for token _pulluser_ and registry _contosoregistry_.

```
TOKEN_PWD=$(az acr token credential generate \
  --name pulluser --registry contosoregistry --expiration-in-days 30 \
  --password1 --query 'passwords[0].value' --output tsv)
```

2. Create a secret with client token credentials. This client token must be linked to your connected registry. For more information, see [Manage client tokens](https://docs.microsoft.com/en-us/azure/container-registry/overview-connected-registry-access#manage-client-tokens).

`kubectl create secret docker-registry regcred --docker-server=<service IP> --docker-username=<client token name> --docker-password=<insert token password> --docker-email=<some email address>`

3. Create a deployment that pulls from the connected registry over HTTP.

```
	cat <<EOF | kubectl apply -f -
	apiVersion: apps/v1
	kind: Deployment
	metadata:
	  name: hello-world-deployment
	  namespace: default
	  labels:
	    app.kubernetes.io/name: "connected-registry"
	spec:
	  replicas: 3
	  selector:
	    matchLabels:
	      app.kubernetes.io/name: "connected-registry"
	  template:
	    metadata:
	      labels:
	        app.kubernetes.io/name: "connected-registry"
	    spec:
	      containers:
	       - name: hello-world
	         image: <service IP>/hello-world:v1
	      imagePullSecrets:
	       - name: regcred
EOF
```

4. Run `kubectl get pods` and see your hello-world image was pulled from the connected registry.

## Uninstall Connected Registry

1. To simulate delete of all resources deployed in the helm release with name "connected-registry", run

`helm uninstall connected-registry --dry-run`

2. To delete all resources deployed in the helm release with name "connected-registry", run

`helm uninstall connected-registry`

3. Deactivate your connected registry resource before deploying it again.

`az acr connected-registry deactivate -r contosoregistry -n myconnectedregistry`
