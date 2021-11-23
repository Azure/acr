---
title: Quickstart - Deploy a connected registry to Kubernetes cluster
description: Use Azure Container Registry CLI and Helm 3 commands to deploy a connected registry to a Kubernetes cluster.
ms.topic: quickstart
ms.date: 11/22/202
ms.author: jeburke
author: jaysterp
ms.custom:
---

# Quickstart:  Deploy a connected registry to Kubernetes cluster

In this quickstart, you use [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-intro) and [Helm 3](https://helm.sh/docs/intro/quickstart/) commands to deploy a connected registry Helm chart to a Kubernetes cluster. You can review the [ACR connected registry introduction](https://docs.microsoft.com/en-us/azure/container-registry/intro-connected-registry) for details about the connected registry feature of Azure Container Registry. For more details on Helm charts, see [Helm documentation](https://helm.sh/docs/topics/charts/).

## Supported Kubernetes distributions

## Prerequisites

- [Install or upgrade Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) to version >= 2.30.0
  - If you're using a local install, sign in with Azure CLI by using the [az login](https://docs.microsoft.com/en-us/cli/azure/reference-index#az_login) command. To finish the authentication process, follow the steps displayed in your terminal. See [Sign in with Azure CLI](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) for additional sign-in options.
- The Azure CLI commands in this article are formatted for the Bash shell. If you're using a different shell like PowerShell or Command Prompt, you may need to adjust line continuation characters or variable assignment lines accordingly. This article uses variables to minimize the amount of command editing required.
- A connected registry resource in Azure as described in the [Create connected registry using the CLI](https://docs.microsoft.com/en-us/azure/container-registry/quickstart-connected-registry-cli) quickstart guide. Both, `ReadWrite` and `ReadOnly` modes will work for this scenario.
- An up-and-running Kubernetes cluster. If you don't have one, you can create a cluster using one of these options:
    - [Kubernetes in Docker](https://kind.sigs.k8s.io/)
    - [K3s: Lightweight Kubernetes](https://rancher.com/docs/k3s/latest/quick-start/) cluster.
    - Self-managed Kubernetes cluster using [Cluster API](https://cluster-api.sigs.k8s.io/user/quick-start.html)
    - An [Azure Kubernetes Service](https://docs.microsoft.com/azure/aks/kubernetes-walkthrough) cluster
- [Helm 3](https://helm.sh/docs/intro/install/) installed.
- [Kubectl](https://kubernetes.io/docs/tasks/tools/#kubectl) installed.
- A `kubeconfig` file and context pointing to your cluster.

## Node Setup Requirements

The helm chart installs Kubernetes resources used to run a connected registry on your Kubernetes cluster. The connected registry runs as a singleton pod on one node of the cluster. The user is responsible for configuring each node of the cluster that will pull from this connected registry. To pull from the connected registry over HTTPS, the user is responsible for configuring SSL certs on each node. To pull from the connected registry of HTTP, the user must update their container runtime settings to recognize the connected registry as "insecure". See more information below.

> [!WARNING]
> Pulling from and pushing to the connected registry over HTTP is not secure. It is recommended to setup TLS certificates.

## Fetch the Connected Registry Helm Chart

From a cluster node, run the following commands to install the connected registry helm chart.

1. Set environment variable to enable OCI artifact support in the Helm 3 client.

`export HELM_EXPERIMENTAL_OCI=1`

2. Pull the connected registry helm chart from MCR

`helm chart pull mcr.microsoft.com/acr/connected-registry/chart:0.1.0`

3. Export the helm chart

`helm chart export mcr.microsoft.com/acr/connected-registry/chart:0.1.0`

4. View the helm chart

`helm show chart connected-registry`

5. Get the connection string for your connected registry. This command generates `password1` of the corresponding sync token resource.

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

## Helm Chart Components

### Storage Class

Decide what storage class resource is appropriate for your Kubernetes distribution. The user is required to provide an existing storage class resource name when deploying the connected registry. You can research your distribution to learn more on predefined storage classes or how to create your own. For instance, see predefined storage resources on AKS at [Concepts - Storage in Azure Kubernetes Services (AKS)](https://docs.microsoft.com/en-us/azure/aks/concepts-storage#storage-classes). To view storage class resources on your cluster, run

`kubectl get sc`

## HTTPS communication with the connected registry

### PKI Certificate Requirements

To establish a secure HTTPS communication with the connected registry, we use PKI certificates during deployment of the connected registry chart. Here are the general requirements for these PKI certs

1. The certificates and keys must be [X.509](https://en.wikipedia.org/wiki/X.509) certificates and [Privacy-Enhanced Mail](https://en.wikipedia.org/wiki/Privacy-Enhanced_Mail) encoded.

2. To configure the connected registry (server) certificate during installation, you must provide
  * A public certificate
  * A private key

> [!IMPORTANT]
> For early proof-of-concept stages, self-signed certificates might be an option but in general, proper PKI certificates signed by a Certificate Authority (CA) should be procured and used.

### Create the PKI certificate

1. Choose a service cluster IP that you will use to deploy the connected registry. The connected registry will be deployed behind a service that is accessible through this IP. Selecting the IP beforehand allows you to create the SSL cert with the IP as the subject alternate name (SAN).

Kubernetes uses a set IP range when deploying services. The IP address that you choose must be in the SERVICE_CLUSTER_IP_RANGE CIDR range that is configured for your cluster. You can view the available service cluster ip range in the kube-controller pod by running

`kubectl get pod kube-controller-manager-<node> -n kube-system -o jsonpath='{.spec.containers[0].command}'`

and viewing the --service-cluster-ip-range setting.

If the selected service IP is invalid, you will see a `422 Unprocessable Entity` HTTP error response from Kubernetes at deployment time.

2. Create self-signed SSL cert with connected-registry service IP as the SAN
  a. `mkdir /certs`
  b. Run 
`openssl req -newkey rsa:4096 -nodes -sha256 -keyout /certs/mycert.key -x509 -days 365 -out /certs/mycert.crt -addext "subjectAltName = IP:<service IP>"`

> [!IMPORTANT]
> For early proof-of-concept stages, self-signed certificates might be an option but in general, proper PKI certificates signed by a Certificate Authority (CA) should be procured and used.

3. Get base64 encoded strings of the certificate and key files

`export TLS_CRT=$(cat mycert.crt | base64 -w0)`
`export TLS_KEY=$(sudo cat mycert.key | base64 -w0) `

### Deploy the Connected Registry

1. Deploy the connected registry, provide your connected registry connection string and existing Kubernetes storage class name. The below command deploys the release "connected-registry". Provide the base64-encoded strings of the public certificate and private key in the  `tls.crt` and `tls.key` values, respectively.

`helm upgrade --namespace connected-registry --create-namespace --install --set connectionString="<insert connection string>" --set pvc.storageClassName="<insert storage class name>" --set image="mcr.microsoft.com/acr/connected-registry:0.6.0" --set tls.crt=$TLS_CRT --set tls.key=$TLS_KEY connected-registry ./connected-registry`

2. To view the deployed connected registry resources, run 

`kubectl get services,deployments,pods,secrets -n connected-registry`

Note: you will see the connected registry service resource running under the cluster IP you selected.

### Configure Each Node

The following steps assume `containerd` is the container runtime of the Kubernetes cluster.

1. Create `/etc/containerd/certs.d/<service IP>:443` directory on each node or server that will access this connected registry. Service IP is that of the connected-registry service.

2. Copy the CA cert corresponding to the SSL cert to this directory.

Note: If using a self-signed cert, copy `mycert.crt` to `ca.crt` and place `ca.crt` in this directory.

	You should have the following file structure on the node:
```
$ tree /etc/containerd/certs.d
/etc/containerd/certs.d
└── <service IP>:443
  └── ca.crt
```
3. Update `/etc/containerd/config.toml` so that containerd can find the directory with the trusted CA cert. Containerd will look in this directory first during runtime operations to the connected registry. 

Paste the following section into the file.

```toml
[plugins."io.containerd.grpc.v1.cri".registry]
  config_path = "/etc/containerd/certs.d"     
  [plugins."io.containerd.grpc.v1.cri".registry.configs."<service IP>:443".tls]
    ca_file   = "/etc/containerd/certs.d/<service IP>:443/ca.crt"
```

4. Restart your container runtime. For containerd, use the following command

`systemctl restart containerd`

### Pull from the Connected Registry

For more information, reference [Pull images from a connected registry](https://docs.microsoft.com/en-us/azure/container-registry/pull-images-from-connected-registry).

1. Get credentials corresponding to a client token linked to the connected registry. For more information, see [Manage client tokens](https://docs.microsoft.com/en-us/azure/container-registry/overview-connected-registry-access#manage-client-tokens). The following example generates `password1` for token _pulluser_ and registry _contosoregistry_.

```
TOKEN_PWD=$(az acr token credential generate \
  --name pulluser --registry contosoregistry --expiration-in-days 30 \
  --password1 --query 'passwords[0].value' --output tsv)
```

2. Create a secret with client token credentials. This client token must be linked to your connected registry. For more information, see [Manage client tokens](https://docs.microsoft.com/en-us/azure/container-registry/overview-connected-registry-access#manage-client-tokens).

`kubectl create secret docker-registry regcred --docker-server=<service IP>:443 --docker-username=jeburkeclient --docker-password=<insert TOKEN_PWD> --docker-email=someemail`

3. Create a deployment that pulls from the connected registry over HTTPS.

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
	         image: <service IP>:443/hello-world:v1
	      imagePullSecrets:
	       - name: regcred
EOF
```

## HTTP (not secure) communication with the connected registry

> [!WARNING]
> Pulling from and pushing to the connected registry over HTTP is not secure. It is recommended to setup SSL certificates. You should use this option only during early stages of development.

### Deploy the Connected Registry

1. Deploy the connected registry, provide your connected registry connection string and existing Kubernetes storage class name. The below command deploys the release "connected-registry".

`helm upgrade --namespace connected-registry --create-namespace --install --set connectionString="<insert connection string>" --set httpEnabled=true --set pvc.storageClassName="<insert storage class name>" --set image="mcr.microsoft.com/acr/connected-registry:0.6.0" connected-registry ./connected-registry`

2. View the deployed connected registry resources

`kubectl get services,deployments,pods,secrets -n connected-registry`

### Configure Each Node

1. Get the cluster IP of the deployed connected registry service. This is the endpoint we will use to pull from the connected registry.

`export SERVICE_IP=$(kubectl get svc {connected registry name} -n connected-registry -o jsonpath='{.spec.clusterIP}')`

2. Add the connected registry endpoint "$(SERVICE_IP):80" as "insecure" per your container runtime settings on **each node** of your cluster that will access this connected registry. For containerd, go to /etc/containerd/config.toml and add the following settings

```toml
[plugins."io.containerd.grpc.v1.cri".registry]
  [plugins."io.containerd.grpc.v1.cri".registry.mirrors]
    [plugins."io.containerd.grpc.v1.cri".registry.mirrors."<service IP>:80"]
      endpoint = ["http://<service IP>:80"]
  [plugins."io.containerd.grpc.v1.cri".registry.configs]
    [plugins."io.containerd.grpc.v1.cri".registry.configs."<service IP>:80".tls]
      insecure_skip_verify = true
```

3. Restart your container runtime. For containerd, use the following command

`systemctl restart containerd`

### Pull from the Connected Registry

For more information, reference [Pull images from a connected registry](https://docs.microsoft.com/en-us/azure/container-registry/pull-images-from-connected-registry).

1. Get credentials corresponding to a client token linked to the connected registry. For more information, see [Manage client tokens](https://docs.microsoft.com/en-us/azure/container-registry/overview-connected-registry-access#manage-client-tokens). The following example generates `password1` for token _pulluser_ and registry _contosoregistry_.

```
TOKEN_PWD=$(az acr token credential generate \
  --name pulluser --registry contosoregistry --expiration-in-days 30 \
  --password1 --query 'passwords[0].value' --output tsv)
```

2. Create a secret with client token credentials. This client token must be linked to your connected registry. For more information, see [Manage client tokens](https://docs.microsoft.com/en-us/azure/container-registry/overview-connected-registry-access#manage-client-tokens).

`kubectl create secret docker-registry regcred --docker-server=<service IP>:80 --docker-username=jeburkeclient --docker-password=<insert TOKEN_PWD> --docker-email=someemail`

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
	         image: <service IP>:80/hello-world:v1
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
