[[_TOC_]]

# Quickstart:  Deploy a connected registry to Kubernetes cluster (Preview)

In this quickstart, you use [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-intro) and [Helm 3](https://helm.sh/docs/intro/quickstart/) commands to deploy a connected registry Helm chart to a Kubernetes cluster. You can review the [ACR connected registry introduction](https://docs.microsoft.com/en-us/azure/container-registry/intro-connected-registry) for details about the connected registry feature of Azure Container Registry. For more details on Helm charts, see [Helm documentation](https://helm.sh/docs/topics/charts/).

## Introduction
In this private preview, the connected registry operates in high availability mode, providing significantly enhanced scalability and performance compared to the public preview version. The registry serivce is now fully scalable, allowing it to efficiently handle increased traffic and workloads. Unlike the public preview, which featured a single, non-scalable component, this version enables dynamic scaling to meet demand, optimizing performance and minimizing latency under heavy load.

One of the key advantages of this architecture is its resilience. Even if one or more components—whether the sync worker, or metadata storage service, or a single registry replica go down, the service remains operational. As long as at least one registry replica is active, the system can continue to serve read requests. This fault tolerance guarantees uninterrupted service, even in the face of partial failures.

In addition to these improvements, the connected registry now supports rolling upgrades. Previously, upgrading required recreating Kubernetes pods, leading to potential service interruptions. With rolling upgrades, updates can be deployed gradually, allowing the system to stay online and serves the read requests while the upgrade is performed, minimizing downtime and disruption.

Finally, the ability to scale components significantly improves overall performance. This distributed architecture allows for better workload balancing across multiple nodes, enhancing capacity and providing faster, more responsive request handling.

## Tools and resources

- [Install or upgrade Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) to version >= 2.63.0.
  - If you're using a local install, sign in with Azure CLI by using the [az login](https://docs.microsoft.com/en-us/cli/azure/reference-index#az_login) command. To finish the authentication process, follow the steps displayed in your terminal. See [Sign in with Azure CLI](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) for additional sign-in options.
- The Azure CLI commands in this article are formatted for the Bash shell. If you're using a different shell like PowerShell or Command Prompt, you may need to adjust line continuation characters or variable assignment lines accordingly. This article uses variables to minimize the amount of command editing required.
- A connected registry resource in Azure as described in the [Create connected registry using the CLI](https://docs.microsoft.com/en-us/azure/container-registry/quickstart-connected-registry-cli) quickstart guide. Both, `ReadWrite` and `ReadOnly` modes will work for this scenario.
- An up-and-running Kubernetes cluster. If you don't have one, you can create a cluster using one of these options:
    - [Kubernetes in Docker](https://kind.sigs.k8s.io/)
    - [K3s: Lightweight Kubernetes](https://rancher.com/docs/k3s/latest/quick-start/) cluster.
    - Self-managed Kubernetes cluster using [Cluster API](https://cluster-api.sigs.k8s.io/user/quick-start.html)
    - An [Azure Kubernetes Service](https://docs.microsoft.com/azure/aks/kubernetes-walkthrough) cluster.
- [Helm 3](https://helm.sh/docs/intro/install/) installed. **Note: the following tutorial is compatible for Helm releases >= v3.11.0.**
- [Kubectl](https://kubernetes.io/docs/tasks/tools/#kubectl) installed.
- A `kubeconfig` file and context pointing to your cluster.

## Concepts in connected registry deployment

The connected registry can be securely deployed using various encryption methods. To ensure a successful deployment, follow the [quickstart](https://learn.microsoft.com/en-us/azure/container-registry/quickstart-connected-registry-cli) guide to review prerequisites and other pertinent information. By default, the connected registry is configured with HTTPS, ReadOnly mode, Trust Distribution, and the Cert Manager service. You can add more customizations and dependencies as needed, depending on your scenario.

### Cert Manager service
The connected registry cert manager is a service that manages TLS certificates for the connected registry in a kubernetes cluster. It ensures secure communication between the connected registry and other components by handling the creation, renewal, and distribution of certificates. This service can be installed as part of the connected registry deployment, or you can use an existing cert manager if it's already installed on your cluster.

[Cert-Manager](https://cert-manager.io/) is an open-source Kubernetes add-on that automates the management and issuance of TLS certificates from various sources. It manages the lifecycle of certificates issued by CA pools created using CA Service, ensuring they are valid and renewed before they expire.

### Trust distribution
Connected registry trust distribution refers to the process of securely distributing trust between the connected registry service and Kubernetes clients within a cluster. This is achieved by using a Certificate Authority (CA), such as cert-manager, to sign TLS certificates, which are then distributed to both the registry service and the clients. This ensures that all entities can securely authenticate each other, maintaining a secure and trusted environment within the Kubernetes cluster.

## Prerequisites

### Service IP
Choose a service cluster IP that you will use to deploy the connected registry. The connected registry will be deployed behind a service that is accessible through this IP.

The clusterIP must be from the cluster subnet IP range. The `service.clusterIP` parameter in helm chart specifies the IP address of the connected registry service within the cluster. It is essential to set the `service.clusterIP` within the range of valid service IPs for the Kubernetes cluster. Ensure that the IP address specified for `service.clusterIP` falls within the designated service IP range defined during the cluster's initial.

> [!NOTE]
> For AKS clusters, the service cluster IP range can be obtained by using the Azure CLI command
> `az aks show -g <Resource Group Name> -n <AKS Cluster Name> --query "networkProfile.serviceCidr" -o tsv`

Confirm that the selected IP is not already in use by running the command:

`kubectl get svc -A`

If the selected service IP is invalid, you will see a `422 Unprocessable Entity` HTTP error response from Kubernetes at deployment time.

### Storage Class
Decide what storage class resource is appropriate for your Kubernetes distribution. The user is required to provide an existing storage class resource name when deploying the connected registry. You can research your distribution to learn more on predefined storage classes or how to create your own. For instance, see predefined storage resources on AKS at [Concepts - Storage in Azure Kubernetes Services (AKS)](https://docs.microsoft.com/en-us/azure/aks/concepts-storage#storage-classes). To view storage class resources on your cluster, run

`kubectl get sc`

> [!IMPORTANT]
> The storage class must be [**ReadWriteMany**](https://kubernetes.io/docs/concepts/storage/persistent-volumes/#access-modes) for connected registry running in high availability mode.
> 

## Common settings
1. Prepare the variables used in the following deployment
The below command generates or refreshes the `password1` of the corresponding sync token resource.
```bash
REGISTRY_NAME=<insert your ACR name>
CONNECTED_REGISTRY_NAME=<insert your connected registry name>
CONNECTION_STRING=$(az acr connected-registry get-settings \
  --registry $REGISTRY_NAME \
  --name $CONNECTED_REGISTRY_NAME \
  --generate-password 1 \
  --parent-protocol https \
  --query "ACR_REGISTRY_CONNECTION_STRING" \
  --output tsv)
STORAGE_CLASS=<insert your read write many storage class name>
CLUSTER_IP=<insert a reserved cluster ip>
REPLICAS=<insert the registry replica numbers to deploy>
SECRET_STRING=<insert any cryptographically secure random secret string>
```

> [!IMPORTANT]
> The only scalable component in a connected registry deployment is the pod with Selector `app.kubernetes.io/name=connected-registry`. All other components **must** use the default replica number provided by the helm chart to avoid unexpected behavior.

## HTTPS communication with connected registry

### Deploy connected registry using built-in cert-manager and trust distribution
By deploying the connected Registry Arc extension, you can synchronize container images and other Open Container Initiative (OCI) artifacts with your ACR registry. The deployment helps speed-up access to registry artifacts and enables the building of advanced scenarios. The deployment ensures secure trust distribution between the connected registry and all client nodes within the cluster, and installs the cert-manager service for Transport Layer Security (TLS) encryption.

This setup uses the built-in cert-manager and trust distribution provided by the connected registry helm chart, enabling you to deploy the connected registry with encryption by following the steps provided:

1. Deploy the connected registry, provide your connected registry connection string and existing Kubernetes storage class name. The below command deploys the release *connected-registry* using default config. By default, the connected registry will install [certificate manager](https://cert-manager.io/) and configure the node to trust the registry automatically. 

> [!NOTE]
> The default settings ensures secure trust distribution between the connected registry and all client nodes within the cluster, and installs the cert-manager service for Transport Layer Security (TLS) encryption.

> [!NOTE]
> The certificate manager(controlled by `cert-manager.enabled`) is enabled by default to sign the certificate, which is used by the kubernetes to pull from the registry.

> [!NOTE]
> The trust distribution(controlled by `trustDistribution.enabled`) is enabled by default to configure the nodes to trust the certificate provided by the registry. Currently, the approach can only configure Debian based Linux nodes.

```bash
helm upgrade \
    --namespace connected-registry \
    --create-namespace \
    --install \
    --set connectionString="$CONNECTION_STRING" \
    --set pvc.storageClassName="$STORAGE_CLASS" \
    --set service.clusterIP="$CLUSTER_IP" \
    --set registry.replicas=$REPLICAS \
    --set registry.uploadSecret="$SECRET_STRING" \
    connected-registry \
    oci://mcr.microsoft.com/acr/connected-registry-ha/chart
```

### Deploy connected registry using your preinstalled cert-manager
This setup gives you control over certificate management, enabling you to deploy the connected registry with encryption with your own pre-installed certificate manager by following the steps provided:

1. Deploy the connected registry by setting `cert-manager.install=false`

```bash
helm upgrade \
    --namespace connected-registry \
    --create-namespace \
    --install \
    --set connectionString="$CONNECTION_STRING" \
    --set pvc.storageClassName="$STORAGE_CLASS" \
    --set service.clusterIP="$CLUSTER_IP" \
    --set registry.replicas=$REPLICAS \
    --set registry.uploadSecret="$SECRET_STRING" \
    --set cert-manager.install=false \
    connected-registry \
    oci://mcr.microsoft.com/acr/connected-registry-ha/chart
```

### Deploy connected registry using bring your own certificate (BYOC)
BYOC allows you to use your own public certificate and private key pair, giving you control over certificate management. This setup enables you to deploy the connected registry with encryption by following the provided steps:

> [!NOTE]
> BYOC is applicable for customers who bring their own certificate that is already trusted by their Kubernetes nodes. It is not recommended to manually update the nodes to trust the certificates.

#### PKI Certificate Requirements
To establish a secure HTTPS communication with the connected registry, we use PKI certificates during deployment of the connected registry chart. Here are the general requirements for these PKI certs:

1. The certificates and keys must be [X.509](https://en.wikipedia.org/wiki/X.509) certificates and [Privacy-Enhanced Mail](https://en.wikipedia.org/wiki/Privacy-Enhanced_Mail) encoded.

2. To configure the connected registry (server) certificate during installation, you must provide:
  - A public certificate.
  - A private key.

#### Deploy connected registry
1. Create self-signed SSL cert with connected-registry service IP as the SAN.
```bash
mkdir /certs
openssl req -newkey rsa:4096 -nodes -sha256 \
  -keyout /certs/mycert.key -x509 -days 365 \ 
  -out /certs/mycert.crt \
  -addext "subjectAltName = IP:$CLUSTER_IP" \
  -addext "subjectAltName = DNS:connected-registry-rstorage.connected-registry.svc.cluster.local"
```
> [!NOTE]
> The certificate is used by the internal services to talk to each other.

1. Get base64 encoded strings of the certificate and key files:
```bash
export TLS_CRT=$(sudo cat /certs/mycert.crt | base64 -w0)
export TLS_KEY=$(sudo cat /certs/mycert.key | base64 -w0)
export CA_CRT=$(sudo cat /certs/mycert.crt | base64 -w0)
```

> [!NOTE]
> The public certificate and private key pair must be encoded in base64 format.

3. Now, you can deploy the connected registry with HTTPS (TLS encryption) using the public certificate and private key pair management by configuring variables set to `cert-manager.enabled=false` and `cert-manager.install=false`. With these parameters, the cert-manager isn't installed or enabled since the public certificate and private key pair is used instead for encryption. Provide the base64-encoded strings of the CA certificate, public certificate and private key in the  `tls.cacrt`, `tls.crt` and `tls.key` values respectively.

```bash
helm upgrade \
    --namespace connected-registry \
    --create-namespace \
    --install \
    --set connectionString="$CONNECTION_STRING" \
    --set pvc.storageClassName="$STORAGE_CLASS" \
    --set service.clusterIP="$CLUSTER_IP" \
    --set registry.replicas=$REPLICAS \
    --set registry.uploadSecret="$SECRET_STRING" \
    --set tls.cacrt=$CA_CRT \
    --set tls.crt=$TLS_CRT \
    --set tls.key=$TLS_KEY \
    --set cert-manager.enabled=false \
    --set cert-manager.install=false \
    connected-registry \
    oci://mcr.microsoft.com/acr/connected-registry-ha/chart
```

### Deploy connected registry with Kubernetes secret management
Kubernetes secret allows you to securely manage authorized access between pods within the cluster. This setup enables you to deploy the connected registry with encryption by following the provided steps:

#### PKI Certificate Requirements
To establish a secure HTTPS communication with the connected registry, we use PKI certificates during deployment of the connected registry chart. Here are the general requirements for these PKI certs:

1. The certificates and keys must be [X.509](https://en.wikipedia.org/wiki/X.509) certificates and [Privacy-Enhanced Mail](https://en.wikipedia.org/wiki/Privacy-Enhanced_Mail) encoded.

2. To configure the connected registry (server) certificate during installation, you must provide:
  - A public certificate.
  - A private key.

#### Deploy connected registry
1. Create self-signed SSL cert with connected-registry service IP as the SAN.
```bash
mkdir /certs
openssl req -newkey rsa:4096 -nodes -sha256 \
  -keyout /certs/mycert.key -x509 -days 365 \ 
  -out /certs/mycert.crt \
  -addext "subjectAltName = IP:$CLUSTER_IP" \
  -addext "subjectAltName = DNS:connected-registry-rstorage.conncted-registry.svc.cluster.local"
```
> [!NOTE]
> The certificate is used by the internal services to talk to each other.

1. Get base64 encoded strings of the certificate and key files:
```bash
export TLS_CRT=$(sudo cat /certs/mycert.crt | base64 -w0)
export TLS_KEY=$(sudo cat /certs/mycert.key | base64 -w0)
export CA_CRT=$(sudo cat /certs/mycert.crt | base64 -w0)
```

> [!NOTE]
> The public certificate and private key pair must be encoded in base64 format.

3. Create k8s secret
```bash
SECRET_NAME=<insert your secret name>
cat <<EOF | kubectl apply -f -
apiVersion: v1
kind: Secret
metadata:
  name: $SECRET_NAME
  namespace: connected-registry
  type: kubernetes.io/tls
data:
  ca.crt: $TLS_CRT
  tls.crt: $TLS_CRT
  tls.key: $TLS_KEY
EOF
```

4. Now, you can deploy the connected registry with HTTPS (TLS encryption) using the Kubernetes secret management by configuring variables set to `cert-manager.enabled=false` and `cert-manager.install=false`. With these parameters, the cert-manager isn't installed or enabled since the Kubernetes secret is used instead for encryption. Provide the secret name in the  `tls.secret` value.

```bash
helm upgrade \
    --namespace connected-registry \
    --create-namespace \
    --install \
    --set connectionString="$CONNECTION_STRING" \
    --set pvc.storageClassName="$STORAGE_CLASS" \
    --set service.clusterIP="$CLUSTER_IP" \
    --set registry.replicas=$REPLICAS \
    --set registry.uploadSecret="$SECRET_STRING" \
    --set tls.secret="$SECRET_NAME" \
    --set cert-manager.enabled=false \
    --set cert-manager.install=false \
    connected-registry \
    oci://mcr.microsoft.com/acr/connected-registry-ha/chart
```

### Deploy connected registry using your own trust distribution and disable the connected registry's default trust distribution
While using your own Kubernetes secret or public certificate and private key pairs, you can deploy the connected registry with TLS encryption, your inherent trust distribution, and reject the connected registry's default trust distribution. This setup enables you to deploy the connected registry with encryption by following the provided steps:

1. Follow the steps to create Kubernetes secret or public certificate, and private key.

2. Deploy the connected registry.

Using BYOC to deploy:
```bash
helm upgrade \
    --namespace connected-registry \
    --create-namespace \
    --install \
    --set connectionString="$CONNECTION_STRING" \
    --set pvc.storageClassName="$STORAGE_CLASS" \
    --set service.clusterIP="$CLUSTER_IP" \
    --set registry.replicas=$REPLICAS \
    --set registry.uploadSecret="$SECRET_STRING" \
    --set trustDistribution.enabled=false \ 
    --set cert-manager.enabled=false \
    --set cert-manager.install=false \ 
    --set tls.cacrt=$CA_CRT \
    --set tls.crt=$TLS_CRT \
    --set tls.key=$TLS_KEY \
    connected-registry \
    oci://mcr.microsoft.com/acr/connected-registry-ha/chart
```

Using k8s secret to deploy:
```bash
helm upgrade \
    --namespace connected-registry \
    --create-namespace \
    --install \
    --set connectionString="$CONNECTION_STRING" \
    --set pvc.storageClassName="$STORAGE_CLASS" \
    --set service.clusterIP="$CLUSTER_IP" \
    --set registry.replicas=$REPLICAS \
    --set registry.uploadSecret="$SECRET_STRING" \
    --set trustDistribution.enabled=false \ 
    --set cert-manager.enabled=false \
    --set cert-manager.install=false \ 
    --set tls.secret=$SECRET_NAME" \
    connected-registry \
    oci://mcr.microsoft.com/acr/connected-registry-ha/chart
```

With these parameters, cert-manager isn't installed or enabled, additionally, the connected registry trust distribution isn't enforced. Instead you're using the cluster provided trust distribution for establishing trust between the Connected registry and the client nodes.

## HTTP (not secure) communication with the connected registry

> [!WARNING]
> Pulling from and pushing to the connected registry over HTTP is not secure. It is recommended to setup SSL certificates. You should use this option only during early stages of development.

### Deploy the Connected Registry

1. Deploy the connected registry, provide your connected registry connection string and existing Kubernetes storage class name. The below command deploys the release *connected-registry* by setting `httpEnabled=true`.

```bash
helm upgrade \
    --namespace connected-registry \
    --create-namespace \
    --install \
    --set connectionString="$CONNECTION_STRING" \
    --set registry.replicas=$REPLICAS \
    --set registry.uploadSecret="$SECRET_STRING" \
    --set httpEnabled=true \
    --set cert-manager.enabled=false \
    --set cert-manager.install=false \
    --set trustDistribution.enabled=false \ 
    --set pvc.storageClassName="$STORAGE_CLASS" \
    connected-registry \
    oci://mcr.microsoft.com/acr/connected-registry-ha/chart
```
## Pull from the Connected Registry

### Verify the deployment
1. Verify the deployment status by running:

`kubectl get services,deployments,pods,secrets -n connected-registry`

and you will see the connected registry service resource running under the cluster IP you selected.
> [!NOTE]
> It can take several minutes to set up certificates and sync with cloud registry when first bootstrapping a connected registry.

For more information, reference [Pull images from a connected registry](https://docs.microsoft.com/en-us/azure/container-registry/pull-images-from-connected-registry).

2. Verify the connected registry status and state

For each connected registry, you can view the status and state of the connected registry using the [az acr connected-registry list](https://learn.microsoft.com/en-us/cli/azure/acr/connected-registry?view=azure-cli-latest#az-acr-connected-registry-list) command:
`az acr connected-registry list --registry $REGISTRY_NAME --output table`

Example Output:
```csv
| NAME | MODE | CONNECTION STATE | PARENT | LOGIN SERVER | LAST SYNC(UTC) |
|------|------|------------------|--------|--------------|----------------|
| myconnectedregistry | ReadWrite | online | myacrregistry | myacrregistry.azurecr.io | 2024-05-09 12:00:00 |
| myreadonlyacr | ReadOnly | offline | myacrregistry | myacrregistry.azurecr.io | 2024-05-09 12:00:00 |
```

3. Verify the specific connected registry details

For details on a specific connected registry, use [az acr connected-registry show](https://learn.microsoft.com/en-us/cli/azure/acr/connected-registry?view=azure-cli-latest#az-acr-connected-registry-show) command:
`az acr connected-registry show --registry $REGISTRY_NAME --name $CONNECTED_REGSITRY_NAME --output table`

Example Output:
```csv
| NAME                | MODE      | CONNECTION STATE | PARENT        | LOGIN SERVER             | LAST SYNC(UTC)      | SYNC SCHEDULE | SYNC WINDOW       |
| ------------------- | --------- | ---------------- | ------------- | ------------------------ | ------------------- | ------------- | ----------------- |
| myconnectedregistry | ReadWrite | online           | myacrregistry | myacrregistry.azurecr.io | 2024-05-09 12:00:00 | 0 0 * * *     | 00:00:00-23:59:59 |
```

The command also provides details on the connected registry's connection status, last sync, sync window, sync schedule, and more.

### Deploy a pod that uses an image from connected registry
To deploy a pod that uses an image from connected registry within the cluster, the operation must be performed from within the cluster node itself. Follow these steps:

1. Get credentials corresponding to a client token linked to the connected registry. For more information, see [Manage client tokens](https://docs.microsoft.com/en-us/azure/container-registry/overview-connected-registry-access#manage-client-tokens).

```bash
TOKEN_NAME=<your ACR token name>
TOKEN_PWD=$(az acr token credential generate \
  --name $TOKEN_NAME \
  --registry $REGISTRY_NAME \
  --expiration-in-days 30 \
  --password1 \
  --query 'passwords[0].value' \
  --output tsv)
```

> [!IMPORTANT]
> Make sure the token is linked to the connected registry.

2. Create a secret in the cluster to authenticate with the connected registry. 
Run the [kubectl create secret docker-registry](https://kubernetes.io/docs/reference/kubectl/generated/kubectl_create/kubectl_create_secret_docker-registry/) command to create a secret in the cluster to authenticate with the connected registry:
```bash
kubectl create secret docker-registry regcred \
    --docker-server=$CLUSTER_IP \
    --docker-username=$TOKEN_NAME \
    --docker-password=$TOKEN_PWD \
    --docker-email=myemail
```

3. Deploy the pod that uses the desired image from the connected registry using the value of `service.clusterIP` of the connected registry, and the image name hello-world with tag latest:

```bash
kubectl apply -f - <<EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hello-world-deployment
  labels:
    app: hello-world
spec:
  selector:
    matchLabels:
      app: hello-world
  replicas: 1
  template:
    metadata:
      labels:
        app: hello-world
    spec:
      imagePullSecrets:
        - name: regcred
      containers:
        - name: hello-world
          image: $CLUSTER_IP/hello-world:latest
EOF
```

4. Run `kubectl get pods` and see your hello-world image was pulled from the connected registry.

## Clean up resources

By deleting the deployed connected registry deployment, you remove the corresponding connected registry pods and configuration settings.

### Delete the connected registry deployment

1. To simulate delete of all resources deployed in the helm release with name *connected-registry*, run

`helm uninstall -n connected-registry connected-registry --dry-run`

2. To delete all resources deployed in the helm release with name *connected-registry*, run

`helm uninstall -n connected-registry connected-registry`

3. Deactivate your connected registry resource before deploying it again:

`az acr connected-registry deactivate -r $REGISTRY_NAME -n $CONNECTED_REGISTRY_NAME`

### Delete the connected registry cloud instance
1. Run the [az acr connected-registry delete](https://learn.microsoft.com/en-us/cli/azure/acr/connected-registry?view=azure-cli-latest#az-acr-connected-registry-delete) command to delete the Connected registry:
```bash
az acr connected-registry delete --registry $REGISTRY_NAME \
--name $CONNECTED_REGISTRY_NAME
```
