---
title: Managed Identity Authentication for Azure Container Registry Connected Registry (Private Preview)
description: Learn step-by-step guidance on configuring a connected registry that authenticates to its parent Azure Container Registry using a user-assigned managed identity during the private preview.
ms.topic: article
ms.date: 10/06/2026
ms.author: zoeyli
author: lizMSFT
ms.service: container-registry
---

# Managed Identity Authentication for Azure Container Registry Connected Registry (Private Preview)

> [!IMPORTANT]
> Managed identity authentication for Azure Container Registry (ACR) connected registry is currently in private preview. As a private preview feature, we encourage users to explore its capabilities in a non-production environment, and avoid production workloads.

Azure Container Registry [connected registry](https://learn.microsoft.com/en-us/azure/container-registry/intro-connected-registry) is an on-premises or edge registry that synchronizes container images and OCI artifacts from a parent registry in Azure. Historically, every connected registry authenticated with its parent registry using a **sync token** — an ACR scope map token whose password had to be generated, embedded in a connection string, distributed to the edge, and rotated manually.

With **managed identity authentication**, a connected registry running on an [Azure Arc-enabled Kubernetes](https://learn.microsoft.com/azure/azure-arc/kubernetes/overview) cluster authenticates to its parent registry using a **user-assigned managed identity** and [Azure Workload Identity](https://learn.microsoft.com/azure/azure-arc/kubernetes/conceptual-workload-identity). No long-lived secret is stored in the connection string, in a Kubernetes secret, or anywhere else in the deployment.

Managed identity authentication builds on [ABAC-enabled repository permissions](https://learn.microsoft.com/azure/container-registry/container-registry-rbac-abac-repository-permissions). Instead of a scope map, the set of repositories a connected registry synchronizes is derived from the Azure RBAC role assignment and ABAC conditions you grant to the managed identity on the parent registry.

Each connected registry has a **sync authentication mode**, set by the `authType` property when the connected registry is created. This article uses the following terms:

* **Sync token mode** (`authType` is `SyncToken`) — synchronization authenticates with an ACR scope map token.
* **Managed identity mode** (`authType` is `ManagedIdentity`) — synchronization authenticates with a user-assigned managed identity, as described in this article.

> [!IMPORTANT]
> Managed identity authentication requires the parent registry to be opted into **ABAC-enabled repository permissions mode**. Registries using legacy registry permissions are not supported. Before continuing, review [ABAC-enabled repository permissions](https://learn.microsoft.com/azure/container-registry/container-registry-rbac-abac-repository-permissions) and understand how opting a registry into ABAC changes the behavior of existing role assignments.

## What changes and what does not

| | Sync token mode (existing) | Managed identity mode (new) |
|---|---|---|
| Credential in the connection string | Token password | None — only a client ID |
| Credential rotation | Manual; requires redeploying the extension | Automatic; uses short-lived tokens |
| Access control model | ACR scope map | Azure RBAC with ABAC conditions |
| Identity provider | ACR-specific token | Microsoft Entra ID |
| Which repositories synchronize | Scope map definition | Derived from the identity's RBAC and ABAC permissions |

> [!NOTE]
> Managed identity applies only to **synchronization between the connected registry and its parent registry**. It does not change how clients access the on-premises connected registry. Docker, ORAS, and other OCI clients continue to authenticate to the local registry using ACR tokens listed in `clientTokenIds`. See [Understand access to a connected registry](https://learn.microsoft.com/en-us/azure/container-registry/intro-connected-registry#client-access).

> [!NOTE]
> Existing connected registries in sync token mode are unaffected. Managed identity mode is new and opt-in, and is selected when the connected registry is created.

## How it works

Managed identity mode relies on three Azure resources that you create before deploying the extension:

1. A **user-assigned managed identity** in Azure, which becomes the connected registry's sync identity.
2. A **connected registry sync role** assigned to that identity on the parent registry, narrowed with an **ABAC condition** to the repositories you want synchronized.
3. A **federated identity credential** on the identity, trusting your Arc-enabled Kubernetes cluster's OIDC issuer and the Kubernetes service account that the connected registry extension creates.

With those in place, the connected registry synchronizes from its parent registry using the managed identity, and the repositories it synchronizes are exactly those the identity is permitted to read. No sync token is created, distributed to the edge, or rotated.

## Key rules for managed identity synchronization

Two rules shape everything else in this article:

* The managed identity's RBAC and ABAC permissions are the **only** definition of sync scope.
* Changing those permissions has **no effect on the edge** until you run `az acr connected-registry resync`.

## Prerequisites

* **Azure CLI 2.91.0 or later**, with the `connectedk8s` and `k8s-extension` extensions installed. Check your CLI version and install or upgrade both extensions:

    ```bash
    az version
    az extension add --upgrade --name connectedk8s
    az extension add --upgrade --name k8s-extension
    ```

    Confirm that `az version` reports Azure CLI 2.91.0 or later and lists both extensions. The extension commands should complete without errors.

* A **Premium** parent registry opted into ABAC-enabled repository permissions, with the dedicated data endpoint enabled.
* Permission to create managed identities, federated identity credentials, and **role assignments** on the registry (`Microsoft.Authorization/roleAssignments/write`).
* A Kubernetes cluster running a [supported distribution](#supported-kubernetes-distributions), connected to Azure Arc, with the ability to restart the Kubernetes API server.
* [Certificate Management for Azure Arc](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview) for TLS certificates, installed in [Step 9](#step-9-install-certificate-management-for-azure-arc). It is the certificate path ACR recommends for new connected registry deployments.

Command examples are formatted for the Bash shell and use variables to minimize editing. In PowerShell or Command Prompt, adjust the line continuation characters and variable assignments accordingly.

### Supported Kubernetes distributions

Managed identity authentication depends on Azure Arc workload identity, which supports only specific Kubernetes distributions. [Workload identity prerequisites](https://learn.microsoft.com/azure/azure-arc/kubernetes/workload-identity#prerequisites) is the authoritative, current list — always check it rather than the snapshot below. At the time of writing, the supported distributions include:

* Ubuntu Linux running **K3s**
* **AKS enabled by Azure Arc**
* **Red Hat OpenShift**
* **VMware Tanzu TKGm**
* **AKS on Edge Essentials**

> [!IMPORTANT]
> A generic Arc-connected Kubernetes cluster is **not** sufficient. If your distribution does not support Azure Arc workload identity, the workload identity webhook will not inject credentials into the connected registry pod and the deployment will fail with a crash loop.

## Subscription preview registration

To enable managed identity authentication for connected registry in private preview for your subscription, follow these steps:

1. **Submit subscription preview registration request.** Begin by submitting a request to register your subscription for the preview. Select the target subscription first, so that the registration does not land on your Azure CLI default subscription:

    ```bash
    az account set --subscription "<subscription-id>"

    az feature register \
    --namespace Microsoft.ContainerRegistry \
    --name ConnectedRegistryManagedIdentity
    ```

    The registration request should be accepted for the selected subscription. Approval is still required before the feature state becomes `Registered`.

2. **Contact the ACR team.** After registering, you need to get registration approval from the ACR team at <acr-pm@microsoft.com>. Reach out to them with the details of your subscription preview registration request, including your subscription ID and intended region.

3. **Verify the registration state.** Do not continue until the feature state is `Registered`. This can take some time after approval.

    ```bash
    az feature show \
    --namespace Microsoft.ContainerRegistry \
    --name ConnectedRegistryManagedIdentity \
    --query properties.state --output tsv
    ```

4. **Propagate preview registration.** After the `ConnectedRegistryManagedIdentity` feature reaches `Registered`, register the `Microsoft.ContainerRegistry` provider again to propagate the change to the subscription:

    ```bash
    az provider register -n Microsoft.ContainerRegistry
    ```

Allow the provider registration to complete before creating a connected registry in managed identity mode.

## Set up the environment

Set the values for your subscription, registry, identity, and cluster before running the remaining commands. The final command selects the subscription for the session.

```bash
SUBSCRIPTION_ID="<subscription-id>"
RG="<resource-group>"
LOCATION="<region>"

ACR="<parent-registry-name>"
CR="<connected-registry-name>"
UAMI="<managed-identity-name>"
CLIENT_TOKEN_NAME="<client-token-name>"

ARC_CLUSTER="<arc-cluster-name>"
EXTENSION_NAME="connected-registry"
NAMESPACE="connected-registry"   # the extension's default release namespace; leave as is
CR_MODE="ReadOnly"   # or ReadWrite

az account set --subscription "$SUBSCRIPTION_ID"
```

## Step 1: Prepare the parent registry

Create a Premium registry opted into ABAC-enabled repository permissions, or use an existing registry that already meets the requirements. Enable the dedicated data endpoint and save the parent registry's resource ID in `ACR_RESOURCE_ID`.

```bash
az acr create \
  --resource-group "$RG" --name "$ACR" --location "$LOCATION" \
  --sku Premium --role-assignment-mode rbac-abac

az acr update --resource-group "$RG" --name "$ACR" --data-endpoint-enabled true

ACR_RESOURCE_ID="$(az acr show --resource-group "$RG" --name "$ACR" --query id --output tsv)"
```

Verify that the registry is opted into ABAC-enabled repository permissions:

```bash
az acr show --name "$ACR" --resource-group "$RG" --query roleAssignmentMode --output tsv
```

The value must be `AbacRepositoryPermissions`. If the value is `LegacyRegistryPermissions`, the registry is not eligible and connected registry creation will be rejected.

> [!IMPORTANT]
> Opting an existing registry into ABAC-enabled repository permissions changes how existing role assignments are evaluated for that registry. Review [ABAC-enabled repository permissions](https://learn.microsoft.com/azure/container-registry/container-registry-rbac-abac-repository-permissions) before opting in an existing registry that serves production traffic.

This guide uses two repositories — one that the managed identity is permitted to synchronize (`hello`) and one that it is not (`app`) — to show that ABAC scoping, not the client token, controls what reaches the edge. If your registry already has repositories, substitute two of your own names throughout and skip this step. Otherwise, seed them:

```bash
az acr import --name "$ACR" --source mcr.microsoft.com/hello-world:latest --image hello:cloud --force
az acr import --name "$ACR" --source mcr.microsoft.com/hello-world:latest --image app:cloud --force
```

Both `hello:cloud` and `app:cloud` should now be present in the parent registry. Only `hello` is included in the managed identity's sync scope later in this guide.

## Step 2: Create the user-assigned managed identity

Create the identity that the connected registry will use for synchronization, and save its resource, client, and principal IDs for later steps:

```bash
az identity create --resource-group "$RG" --name "$UAMI" --location "$LOCATION"

UAMI_RESOURCE_ID="$(az identity show --resource-group "$RG" --name "$UAMI" --query id --output tsv)"
UAMI_CLIENT_ID="$(az identity show --resource-group "$RG" --name "$UAMI" --query clientId --output tsv)"
UAMI_PRINCIPAL_ID="$(az identity show --resource-group "$RG" --name "$UAMI" --query principalId --output tsv)"
```

The identity creation should succeed, and all three ID variables should contain values.

> [!IMPORTANT]
> The identity binding on a connected registry is **immutable**. Create the identity you intend to keep. Changing or replacing the identity later requires recreating the connected registry.

## Step 3: Grant the managed identity permission on the parent registry

Assign one of the built-in connected registry sync roles to the managed identity, matching the connected registry's mode.

| Connected registry mode | Role |
|---|---|
| `ReadOnly` | `Container Registry Connected Registry Sync Reader` |
| `ReadWrite` | `Container Registry Connected Registry Sync Contributor` |

These roles grant the gateway actions required for activation and message exchange, plus repository content and metadata actions. The gateway actions are not repository-scoped; the repository actions are, so you narrow them with an ABAC condition.

### Define the repository condition

The example below grants repository actions only for the single repository named `hello`, and leaves the non-repository gateway actions unrestricted.

```bash
CONDITION="$(cat <<'EOF' | tr -d '\n'
(
 (
  !(ActionMatches{'Microsoft.ContainerRegistry/registries/repositories/content/read'})
  AND
  !(ActionMatches{'Microsoft.ContainerRegistry/registries/repositories/content/write'})
  AND
  !(ActionMatches{'Microsoft.ContainerRegistry/registries/repositories/content/delete'})
  AND
  !(ActionMatches{'Microsoft.ContainerRegistry/registries/repositories/metadata/read'})
  AND
  !(ActionMatches{'Microsoft.ContainerRegistry/registries/repositories/metadata/write'})
  AND
  !(ActionMatches{'Microsoft.ContainerRegistry/registries/repositories/metadata/delete'})
 )
 OR
 (
  @Request[Microsoft.ContainerRegistry/registries/repositories:name] StringEquals 'hello'
 )
)
EOF
)"
```

> [!IMPORTANT]
> To grant a whole repository namespace instead of a single repository, replace the `StringEquals` comparison with a prefix match and **include the trailing slash** — for example `StringStartsWith 'application/frontend/'`. Without the trailing slash, the condition matches too broadly, granting permissions to unintended repositories such as `application/frontendv1`. ABAC conditions are case-sensitive, so consider `StringStartsWithIgnoreCase` with lowercase characters to avoid case-related mismatches. For more condition authoring guidance, including the Azure portal condition builder, see [scope role assignment to a specific repository](https://learn.microsoft.com/azure/container-registry/container-registry-rbac-abac-repository-permissions?tabs=azure-portal#scope-role-assignment-to-a-specific-repository).

### Create the role assignment

Assign the role to the managed identity at the **registry scope**. Derive the role from `CR_MODE` so that the role and the connected registry mode cannot drift apart:

```bash
if [ "$CR_MODE" = "ReadWrite" ]; then
  SYNC_ROLE="Container Registry Connected Registry Sync Contributor"
else
  SYNC_ROLE="Container Registry Connected Registry Sync Reader"
fi

az role assignment create \
  --role "$SYNC_ROLE" \
  --scope "$ACR_RESOURCE_ID" \
  --assignee-object-id "$UAMI_PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --condition "$CONDITION" \
  --condition-version "2.0" \
  --description "Connected registry sync access"
```

The resulting role assignment should show the selected sync role at the parent registry's scope, with the ABAC condition and condition version `2.0`.

> [!WARNING]
> Assign the sync role at **registry scope**, not at subscription or resource group scope. In managed identity mode, the identity's permissions are the only source of truth for the set of repositories that synchronize. A broader inherited role assignment silently widens the sync scope. Assigning a sync role **without** an ABAC condition grants permissions to all repositories in the registry.

> [!IMPORTANT]
> Azure RBAC changes can take **up to 10 minutes** to propagate. Deploying the connected registry extension before propagation completes causes activation to fail with `403`. Either wait before deploying, or be prepared to restart the pod. See [Troubleshooting](#troubleshooting).

## Step 4: Create a client token for local access

Managed identity covers synchronization only. Create an ACR token for clients that pull from or push to the on-premises connected registry. The command saves the generated password in `CLIENT_PASSWORD` without printing it:

```bash
CLIENT_PASSWORD="$(az acr token create \
  --name "$CLIENT_TOKEN_NAME" --registry "$ACR" \
  --repository hello content/read metadata/read \
  --repository app content/read metadata/read \
  --query credentials.passwords[0].value --output tsv)"
```

> [!NOTE]
> `clientTokenIds` controls which ACR tokens clients may use against the local connected registry. It does **not** grant the managed identity any synchronization permission. Synchronization permission comes solely from the role assignment created in Step 3.

> [!IMPORTANT]
> `CLIENT_PASSWORD` is a long-lived credential for local client access. Managed identity removes the *sync* secret, not this one. Do not enable shell tracing, echo it, write it to a file, or paste it into logs or issue reports. Pass it with `--password-stdin` as shown later, run `unset CLIENT_PASSWORD` when you finish, and regenerate the token with `az acr token credential generate` if it is ever exposed.

> [!NOTE]
> The token above deliberately grants local access to `app`, which the managed identity's ABAC condition **excludes** from synchronization. This is a negative test: it demonstrates that client token access and sync scope are independent. Pulling `app:cloud` from the edge is expected to return `404` until you widen the ABAC condition and run `az acr connected-registry resync`.

## Step 5: Create the connected registry

With the identity and client token ready, create the connected registry with `az acr connected-registry create`. The `--auth-type ManagedIdentity` and `--identity` parameters require Azure CLI 2.91.0 or later. The command reuses the `CR_MODE` value you set in [Set up the environment](#set-up-the-environment), which must match the sync role assigned in [Step 3](#step-3-grant-the-managed-identity-permission-on-the-parent-registry).

```bash
az acr connected-registry create \
  --registry "$ACR" --name "$CR" \
  --mode "$CR_MODE" \
  --auth-type ManagedIdentity \
  --identity "$UAMI_RESOURCE_ID" \
  --client-tokens "$CLIENT_TOKEN_NAME" \
  --log-level Information \
  --sync-message-ttl P2D
```

Confirm the result:

```bash
az acr connected-registry show --registry "$ACR" --name "$CR" \
  --query "{authType:parent.syncProperties.authType, tokenId:parent.syncProperties.tokenId, identity:identity}"
```

Expect `authType` to be `ManagedIdentity`, `tokenId` to be absent, and your managed identity resource ID to appear.

## Step 6: Connect the cluster to Azure Arc with OIDC and workload identity

Follow the [Azure Arc quickstart](https://learn.microsoft.com/azure/azure-arc/kubernetes/quickstart-connect-cluster?tabs=azure-cli) to prepare the cluster, then connect it with both flags:

```bash
az connectedk8s connect \
  --name "$ARC_CLUSTER" --resource-group "$RG" --location "$LOCATION" \
  --enable-oidc-issuer --enable-workload-identity
```

If the cluster is already connected to Azure Arc without OIDC and workload identity enabled, update it instead:

```bash
az connectedk8s update \
  --name "$ARC_CLUSTER" --resource-group "$RG" \
  --enable-oidc-issuer --enable-workload-identity
```

Verify that the cluster is connected:

```bash
az connectedk8s show --name "$ARC_CLUSTER" --resource-group "$RG" \
  --query connectivityStatus --output tsv
```

The expected result is `Connected`.

## Step 7: Align the Kubernetes API server service account issuer

Read the Arc OIDC issuer:

```bash
OIDC_ISSUER="$(az connectedk8s show --resource-group "$RG" --name "$ARC_CLUSTER" \
  --query oidcIssuerProfile.issuerUrl --output tsv)"
echo "$OIDC_ISSUER"
```

Confirm that `OIDC_ISSUER` contains the Arc cluster's public HTTPS issuer URL before continuing.

Configure your API server to advertise this issuer. The exact procedure depends on your Kubernetes distribution — follow [configure workload identity settings on the Kubernetes cluster](https://learn.microsoft.com/azure/azure-arc/kubernetes/workload-identity#configure-workload-identity-settings-on-the-kubernetes-cluster), which covers each supported distribution.

Then verify the issuer advertised by the API server:

```bash
ADVERTISED_ISSUER="$(kubectl get --raw='/.well-known/openid-configuration' | jq -r '.issuer')"
printf 'Arc issuer: %s\nAPI server issuer: %s\n' "$OIDC_ISSUER" "$ADVERTISED_ISSUER"
```

The two values must match, apart from a possible trailing slash. If the API server still reports `https://kubernetes.default.svc.cluster.local`, stop here — the remaining steps appear to succeed, but the connected registry never authenticates.

## Step 8: Create the federated identity credential

The connected registry extension's Helm chart creates its workload identity service account deterministically as **`<extension-name>-wi-sa`** in the extension's release namespace, where `<extension-name>` is the value you pass to `az k8s-extension create --name`. The federated identity credential must exist *before* the pod starts, so use the extension name and default release namespace set in [Set up the environment](#set-up-the-environment) consistently.

```bash
SERVICE_ACCOUNT="${EXTENSION_NAME}-wi-sa"
FIC_SUBJECT="system:serviceaccount:${NAMESPACE}:${SERVICE_ACCOUNT}"

az identity federated-credential create \
  --resource-group "$RG" --identity-name "$UAMI" --name "${CR}-fic" \
  --issuer "$OIDC_ISSUER" \
  --subject "$FIC_SUBJECT" \
  --audiences "api://AzureADTokenExchange"
```

The created credential should trust the Arc OIDC issuer, the `system:serviceaccount:<namespace>:<extension-name>-wi-sa` subject, and the `api://AzureADTokenExchange` audience.

> [!IMPORTANT]
> If you choose a different extension name or namespace, use those same values when building `FIC_SUBJECT` and when deploying the extension. A mismatch prevents the connected registry pod from obtaining a Microsoft Entra ID token.

## Step 9: Install Certificate Management for Azure Arc

The connected registry serves its endpoint over HTTPS, so it needs a TLS certificate. By default, the connected registry extension installs its own bundled cert-manager.

For **new** connected registry deployments on Azure Arc-enabled Kubernetes, ACR recommends **[Certificate Management for Azure Arc](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview)** — the Microsoft-managed cert-manager and trust-manager distribution — together with `cert-manager.install=false`, which tells the connected registry extension not to install its bundled copy.

**Existing** deployments can keep using the bundled cert-manager or their own certificates, as described in [Deploy the connected registry Arc extension](https://learn.microsoft.com/azure/container-registry/tutorial-connected-registry-arc). Guidance for moving existing deployments to Certificate Management for Azure Arc will follow.

Confirm that your cluster's region is in the [list of supported regions](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview#regional-support), then install the extension:

```bash
az k8s-extension create \
  --resource-group "$RG" \
  --cluster-name "$ARC_CLUSTER" \
  --cluster-type connectedClusters \
  --name "azure-cert-management" \
  --extension-type "microsoft.certmanagement"
```

Wait for the Certificate Management for Azure Arc extension to provision successfully and its certificate components to become ready before continuing.

## Step 10: Deploy the connected registry Arc extension

Generate the protected settings file holding the connection string. The command writes `protected-settings-extension.json` without displaying its contents. Keep the file private: although managed identity removes the sync password, the connection string still contains deployment settings.

```bash
cat << EOF > protected-settings-extension.json
{
  "connectionString": "$(az acr connected-registry get-settings \
  --name "$CR" \
  --registry "$ACR" \
  --parent-protocol https \
  --query ACR_REGISTRY_CONNECTION_STRING --output tsv)"
}
EOF
```

Choose the cluster IP that the connected registry service will use, and export it. The address must fall inside the cluster's service IP range (service CIDR) and must not already be in use by another service:

```bash
CR_CLUSTER_IP="<cluster-ip-in-service-cidr>"

kubectl get services --all-namespaces --output wide
```

Check the listed service IP addresses to confirm that `CR_CLUSTER_IP` is not already assigned.

Deploy the extension:

```bash
az k8s-extension create \
  --name "$EXTENSION_NAME" \
  --cluster-name "$ARC_CLUSTER" --resource-group "$RG" \
  --cluster-type connectedClusters \
  --extension-type Microsoft.ContainerRegistry.ConnectedRegistry \
  --auto-upgrade-minor-version true \
  --config service.clusterIP="$CR_CLUSTER_IP" \
  --config cert-manager.install=false \
  --config-protected-file protected-settings-extension.json
```

The extension should provision successfully. Confirm that the connected registry comes online in [Validate the deployment](#validate-the-deployment); successful provisioning alone does not prove that workload identity authentication is working.

> [!NOTE]
> Managed identity support ships in connected registry Arc extension **1.5.0**, so leave auto-upgrade on. To pin a specific version instead, use `--version <version number>` with `--auto-upgrade-minor-version false`. Deploying an earlier extension version does not enable managed identity authentication, and the connected registry falls back to expecting a sync token in its connection string.

> [!IMPORTANT]
> `cert-manager.install=false` is required when you use Certificate Management for Azure Arc. It is **not** the extension's default. If you omit it, the extension installs its own bundled cert-manager, which collides with the one installed in [Step 9](#step-9-install-certificate-management-for-azure-arc) and the deployment fails. This is the same configuration documented for a preinstalled cert-manager in [Secure deployment options for the connected registry extension](https://learn.microsoft.com/azure/container-registry/tutorial-connected-registry-arc).

The extension has two separate cert-manager settings, and the distinction matters:

| Setting | Default | Meaning |
|---|---|---|
| `cert-manager.enabled` | `true` | The extension creates cert-manager `Issuer` and `Certificate` resources to obtain its TLS certificate. Leave this `true`. |
| `cert-manager.install` | `true` | The extension also installs its own bundled copy of cert-manager. Set this to `false` so that Certificate Management for Azure Arc serves the request instead. |

> [!NOTE]
> Leave `cert-manager.enabled` at its default of `true`. Setting it to `false` means the extension no longer requests a certificate at all, and you must then supply your own certificate through `tls.secret`, or through `tls.crt` and `tls.key`. Setting `cert-manager.install=true` while `cert-manager.enabled=false` is rejected by the chart.

## Validate the deployment

### Verify the Azure resource

```bash
az acr connected-registry show --registry "$ACR" --name "$CR" \
  --query "{authType:parent.syncProperties.authType, tokenId:parent.syncProperties.tokenId, identity:identity, status:status, connectionState:connectionState}"
```

Expect `authType` to be `ManagedIdentity`, `tokenId` to be absent, and — once the edge deployment is healthy — `connectionState` to be `Online`.

### Verify workload identity injection

```bash
kubectl get serviceaccount "$SERVICE_ACCOUNT" --namespace "$NAMESPACE" \
  --output jsonpath='{.metadata.annotations.azure\.workload\.identity/client-id}'

echo "$UAMI_CLIENT_ID"
```

The two values must match.

Then inspect the pod:

```bash
POD="$(kubectl get pods --namespace "$NAMESPACE" \
  -l app.kubernetes.io/name=connected-registry \
  -o jsonpath='{.items[0].metadata.name}')"

kubectl exec --namespace "$NAMESPACE" "$POD" -- env | grep AZURE_
kubectl describe pod --namespace "$NAMESPACE" "$POD" | grep -i azure-identity-token
```

A correctly injected pod has `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_FEDERATED_TOKEN_FILE`, and `AZURE_AUTHORITY_HOST` set, plus a projected `azure-identity-token` volume. If these are missing, workload identity is not correctly configured. See [Troubleshooting](#troubleshooting).

### Verify a client pull

Confirm the pod is running and the logs show successful activation:

```bash
kubectl get pods --namespace "$NAMESPACE"
kubectl logs --namespace "$NAMESPACE" "$POD" | head -50
```

The connected registry pod should be running, and its startup logs should show successful activation rather than an authentication error. If activation fails, see [Troubleshooting](#troubleshooting).

The connected registry uses a `ClusterIP` service. Run the following commands on a cluster node to discover its endpoint:

```bash
CR_SERVICE="$(kubectl --namespace "$NAMESPACE" get services \
  --output jsonpath='{range .items[*]}{.metadata.name}{"\n"}{end}' | grep -v cert-manager | head -1)"
CR_SERVICE_IP="$(kubectl --namespace "$NAMESPACE" get service "$CR_SERVICE" --output jsonpath='{.spec.clusterIP}')"
CR_SERVICE_PORT="$(kubectl --namespace "$NAMESPACE" get service "$CR_SERVICE" --output jsonpath='{.spec.ports[0].port}')"
CR_ENDPOINT="${CR_SERVICE_IP}:${CR_SERVICE_PORT}"
```

The commands save the connected registry service's cluster IP and port in `CR_ENDPOINT` for the client pull; they do not print the endpoint.

Create a pull secret from the client token created in [Step 4](#step-4-create-a-client-token-for-local-access), then deploy a pod that uses the secret to pull a synchronized image from the connected registry:

```bash
kubectl create secret docker-registry regcred \
  --docker-server="$CR_ENDPOINT" \
  --docker-username="$CLIENT_TOKEN_NAME" \
  --docker-password="$CLIENT_PASSWORD"
```

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
          image: ${CR_ENDPOINT}/hello:cloud
EOF

kubectl rollout status deployment/hello-world-deployment --timeout=120s
```

A successful rollout proves the whole chain: the managed identity authenticated to the parent registry, the ABAC condition allowed `hello`, the image synchronized to the edge, and the client token authorized the pull. No TLS flags are needed — the certificate covers the `service.clusterIP` you set in [Step 10](#step-10-deploy-the-connected-registry-arc-extension), and trust distribution already placed the connected registry's CA in the node's container runtime trust store.

If the pod reports `ImagePullBackOff`, inspect the pull error:

```bash
kubectl describe pod -l app=hello-world | grep -A5 Events
```

Look for the pull failure in the pod events:

A `401` indicates a client token, password, repository scope, or `clientTokenIds` problem. A `404` usually means authentication succeeded but the artifact has not synchronized yet, or is excluded by the managed identity's ABAC condition.

To confirm the ABAC condition is actually narrowing the sync scope, repeat the deployment with `${CR_ENDPOINT}/app:cloud`. It is expected to fail with a `404`, because the condition deliberately excludes the `app` repository.

When you finish validating, remove the test resources and clear the client credential from your shell:

```bash
kubectl delete deployment hello-world-deployment
kubectl delete secret regcred
unset CLIENT_PASSWORD
```

The test deployment and pull secret should be removed, and `CLIENT_PASSWORD` should no longer be set in your shell.

## Troubleshooting

The primary diagnostic surface for connected registries in managed identity mode is the pod log on the Arc-enabled Kubernetes cluster:

```bash
kubectl logs --namespace "$NAMESPACE" "$POD"
kubectl logs --namespace "$NAMESPACE" "$POD" --previous   # for a crashed container
```

Use the first command for the current container's log. If the container restarted, the second command shows the previous container's log, where the activation failure might appear.

### Symptom matrix

| Symptom | Likely cause | Resolution |
|---|---|---|
| Pod in `CrashLoopBackOff`; log reports that managed identity configuration is incomplete and that `AZURE_TENANT_ID` and `AZURE_FEDERATED_TOKEN_FILE` are required | Workload identity was never injected. The OIDC issuer or workload identity is not enabled on the Arc cluster, or the distribution is unsupported | [Common fixes](#common-fixes) |
| Log reports that the federated token file does not exist | The webhook added the environment variables but the projected volume is missing, or the pod predates webhook installation | Delete the pod so it is recreated through the admission webhook |
| Log shows `AADSTS70021: No matching federated identity record found for presented assertion` | The federated identity credential's issuer or subject does not match the cluster | [Common fixes](#common-fixes) |
| The API server reports its issuer as `https://kubernetes.default.svc.cluster.local` | The API server service account issuer was never aligned to the Arc OIDC issuer | Repeat [Step 7](#step-7-align-the-kubernetes-api-server-service-account-issuer), then restart the pod |
| Activation fails with HTTP 403; the startup log reports `status: Forbidden` and that the connected registry instance failed to activate. The pod terminates and restarts | The sync role assignment is missing, or has not finished propagating | Verify the assignment, wait up to 10 minutes, then delete the pod |
| Pod is healthy but no repositories synchronize | The ABAC condition excludes every repository, or the role grants gateway actions only | Review and correct the condition, then run `az acr connected-registry resync` |
| Some repositories synchronize and others do not | The ABAC condition does not match those repository names | Update the condition, then resync |
| Synchronization worked and then stopped | The role assignment was removed or narrowed, or the Arc OIDC issuer changed | If the narrowing was accidental, restore permissions and resync. If it was intentional, resync so the edge catalog is rebuilt from the current permissions. If the cluster was rebuilt, see [Common fixes](#common-fixes) |
| Synchronization resumed after a permissions fix, but some artifacts pushed during the outage are still missing | Individual push, delete, and tag events that failed with `403` are classified as non-retryable and are dropped. Restarting the pod does not replay them | Run `az acr connected-registry resync --registry "$ACR" --name "$CR"` to reconcile the full catalog |
| Connected registry creation fails with an error stating that managed identity sync is only supported on ABAC-enabled registries | The parent registry uses legacy registry permissions | Opt the registry into ABAC-enabled repository permissions |
| Connected registry creation fails because the feature is not registered | The `ConnectedRegistryManagedIdentity` feature is not registered on the subscription | Complete [Subscription preview registration](#subscription-preview-registration) |
| ARM rejects the request with a `400` on the `identity` property | The managed identity does not exist, belongs to a different tenant, is system-assigned, or more than one identity was supplied | Supply exactly one existing user-assigned managed identity |
| Extension installation fails on cert-manager CRDs or webhooks, reporting that a resource already exists or is owned by another release | Two cert-manager installations collide, because the extension installed its bundled copy alongside Certificate Management for Azure Arc | Delete the extension, then redeploy it with `--config cert-manager.install=false`. See [Step 10](#step-10-deploy-the-connected-registry-arc-extension) |
| The connected registry pod stays `Pending` or `0/1 Ready`, and its TLS secret stays empty | The extension created `Certificate` resources, but no cert-manager is present to issue them, because `cert-manager.install=false` was set without installing Certificate Management for Azure Arc | Install Certificate Management for Azure Arc. See [Step 9](#step-9-install-certificate-management-for-azure-arc). Then inspect the request with `kubectl describe certificate -n $NAMESPACE` |

### Common fixes

* **Workload identity was never injected.** Credential injection happens at pod admission, so you rarely need to delete the extension. Fix the cluster ([Step 6](#step-6-connect-the-cluster-to-azure-arc-with-oidc-and-workload-identity) and [Step 7](#step-7-align-the-kubernetes-api-server-service-account-issuer)), create the federated identity credential ([Step 8](#step-8-create-the-federated-identity-credential)), then recreate the pod and re-run the [workload identity verification](#verify-workload-identity-injection):

    ```bash
    kubectl rollout restart deployment "$EXTENSION_NAME" --namespace "$NAMESPACE"
    ```

* **Federated identity credential mismatch.** Compare the credential against the cluster's actual values:

    ```bash
    az identity federated-credential list --resource-group "$RG" --identity-name "$UAMI" \
      --query "[].{name:name, issuer:issuer, subject:subject, audiences:audiences}" --output table

    az connectedk8s show --resource-group "$RG" --name "$ARC_CLUSTER" \
      --query oidcIssuerProfile.issuerUrl --output tsv
    ```

    Compare the reported values: `subject` must be exactly `system:serviceaccount:<namespace>:<extension-name>-wi-sa`, `issuer` must equal the Arc OIDC issuer, and `audiences` must contain `api://AzureADTokenExchange`. Update or recreate the credential, then restart the pod. If the cluster was rebuilt and the Arc OIDC issuer URL changed, recreate the credential with the new issuer.

* **Collect more detail.** Update the connected registry resource with `"logging": { "logLevel": "Debug" }`. Access tokens are never written to logs; still redact connection strings, tokens, and passwords when sharing them.

Delete and recreate the extension only if the extension name, namespace, connection string, or managed identity client ID is wrong, or the extension is stuck in a failed provisioning state. Then repeat [Step 10](#step-10-deploy-the-connected-registry-arc-extension).

> [!WARNING]
> Deleting the extension uninstalls the connected registry and interrupts local registry service for every client on the cluster. It also removes chart-managed resources, including the persistent volume claim holding synchronized artifacts and the issued TLS certificates. Prefer restarting the deployment or updating the extension in place whenever the release is recoverable.

## Migrate an existing connected registry to managed identity

An existing connected registry in sync token mode can be migrated to managed identity mode. The migration is **one-way and final**.

### Migration rules

| Rule | Detail |
|---|---|
| Direction | `SyncToken` to `ManagedIdentity` only. The reverse migration is rejected. |
| State | The connected registry must be deactivated and offline before migrating. |
| Topology | Must be a top-level connected registry with no child connected registries. |
| Registry | The parent registry must be opted into ABAC-enabled repository permissions. |
| Immutability | After migration, the managed identity cannot be changed or replaced. |
| Other changes | Changing `tokenId` on a connected registry in sync token mode, or swapping one managed identity for another, is rejected. |

### Migration procedure

1. Complete [Step 2](#step-2-create-the-user-assigned-managed-identity), [Step 3](#step-3-grant-the-managed-identity-permission-on-the-parent-registry), [Step 6](#step-6-connect-the-cluster-to-azure-arc-with-oidc-and-workload-identity), [Step 7](#step-7-align-the-kubernetes-api-server-service-account-issuer), and [Step 8](#step-8-create-the-federated-identity-credential): create the managed identity, assign the sync role with an ABAC condition, enable the OIDC issuer and workload identity, align the API server service account issuer, and create the federated identity credential.

2. Decide how to manage certificates before updating the extension. If the existing deployment uses the extension's bundled cert-manager, keep using it by omitting `cert-manager.install=false` in migration step 5, or move to Certificate Management for Azure Arc by completing [Step 9](#step-9-install-certificate-management-for-azure-arc) first. Running both at once collides on the cert-manager CRDs and webhooks.

3. Deactivate the connected registry and confirm that it is offline:

    ```bash
    az acr connected-registry deactivate --registry "$ACR" --name "$CR" --yes

    az acr connected-registry show --registry "$ACR" --name "$CR" \
      --query "{status:status, connectionState:connectionState}"
    ```

    Confirm that `connectionState` reports `Offline` before continuing. Changing the authentication mode while the connected registry is still online is rejected.

4. Update the connected registry to managed identity authentication:

    ```bash
    az acr connected-registry update \
      --registry "$ACR" --name "$CR" \
      --auth-type ManagedIdentity \
      --identity "$UAMI_RESOURCE_ID"
    ```

    The update should succeed. The cloud resource now uses managed identity mode, but the edge cannot reconnect until its extension receives the new connection string.

5. Retrieve the connection string in managed identity format and update the existing Arc extension in place so the edge deployment uses the new credentials:

    ```bash
    cat << EOF > protected-settings-extension.json
    {
      "connectionString": "$(az acr connected-registry get-settings \
      --name "$CR" \
      --registry "$ACR" \
      --parent-protocol https \
      --query ACR_REGISTRY_CONNECTION_STRING --output tsv)"
    }
    EOF

    az k8s-extension update \
      --name "$EXTENSION_NAME" \
      --cluster-name "$ARC_CLUSTER" --resource-group "$RG" \
      --cluster-type connectedClusters \
      --config-protected-file protected-settings-extension.json
    ```

    The extension update should succeed with the new protected settings. Confirm that the connected registry reconnects by following [Validate the deployment](#validate-the-deployment).

    Add `--config cert-manager.install=false` only if you migrated to Certificate Management for Azure Arc in step 2. If the extension cannot be updated in place — for example because its release namespace is wrong for the federated identity credential subject — delete and recreate it instead, observing the warning in [Common fixes](#common-fixes).

6. Validate the deployment. See [Validate the deployment](#validate-the-deployment).

> [!IMPORTANT]
> The cloud resource and the deployed Arc extension must use the same authentication mode. Update both, or the connected registry will not reconnect.

## Private preview limitations of managed identity authentication

The following limitations apply during the private preview of managed identity authentication for connected registry.

### Feature limitations

1. **ABAC-enabled registries only.** Managed identity requires a parent registry opted into ABAC-enabled repository permissions. Registries using legacy registry permissions are not supported.
2. **Top-level connected registries only.** A connected registry in managed identity mode cannot have child connected registries, and a connected registry whose parent is another connected registry must remain in sync token mode.
3. **Exactly one user-assigned managed identity.** System-assigned managed identities are not supported.
4. **The identity binding is immutable** after the connected registry is created.
5. **Migration is one-way**, from sync token mode to managed identity mode only.
6. **Sync scope is derived from RBAC and ABAC permissions.** There is no explicit repository filter field, and no API reports the effective sync scope. Role assignments inherited from subscription or resource group scope silently widen it, so audit them:

    ```bash
    az role assignment list --assignee "$UAMI_PRINCIPAL_ID" --all --include-inherited --output table
    ```

    Check the output for inherited or unconditioned assignments that grant access to more repositories than intended.

7. **Permission changes require a manual resync.** Role assignment and ABAC condition changes are not detected automatically. Narrowing permissions stops future synchronization immediately, but artifacts already on the edge remain available until a resync rebuilds the permitted catalog from the identity's current permissions:

    ```bash
    az acr connected-registry resync --registry "$ACR" --name "$CR"
    ```

    After the resync completes, the edge catalog should reflect the identity's current repository permissions, including removals.

### Tooling and operational notes

1. **The Azure portal can display a connected registry in managed identity mode, but cannot create one** or perform the migration. Use the Azure CLI.
2. **`az acr connected-registry permissions` applies only to connected registries in sync token mode**, because its output is derived from a scope map.
3. **`--cleanup` has no effect.** Deleting the connected registry leaves the managed identity and its role assignments in place.
4. If the managed identity loses gateway permissions after activation, synchronization stalls silently, and recovery begins only after `messageTtl` elapses.
5. Full synchronization lists the parent registry's entire catalog and then probes repository permissions in batches. A parent registry with far more repositories than the connected registry is permitted to read still pays the full catalog cost on every resync.

## Reporting issues and asking for help

To report an issue with this preview, [create a new bug](https://github.com/Azure/acr/issues/new?assignees=&labels=connected-registry,bug&template=bug_report.md&title=) in this repository, or contact the ACR team at <acr-pm@microsoft.com>. The feature owners triage these reports.

If an issue affects your **parent registry** — for example synchronization failures, throttling, or anything impacting production traffic — open an Azure support case as you normally would, and mention that the connected registry is enrolled in the managed identity mode private preview.

When reporting a problem, please include:

* Subscription ID, registry name, connected registry name, and region
* The UTC timestamp of the failure, and any correlation ID from the ARM response
* The connected registry extension version
* Kubernetes distribution and version, and the Azure Arc agent version
* Relevant pod logs, **with connection strings, tokens, and passwords redacted**
* What you expected to happen, and what happened instead

We are especially interested in feedback on:

* The RBAC and ABAC-derived sync scope model, and whether an explicit repository filter would serve you better
* The manual resync requirement after permission changes
* Setup friction, particularly the API server service account issuer alignment step
* Which Kubernetes distributions you need supported

## Conclusion

Managed identity authentication removes the last long-lived secret from the connected registry synchronization path. Instead of distributing and rotating sync token passwords, you grant a user-assigned managed identity a sync role scoped with ABAC conditions, and the edge workload acquires short-lived Microsoft Entra ID tokens through Azure Arc workload identity.

## Next steps

* [Overview of connected registry](./intro-connected-registry.md)
* [Understand access to a connected registry](./overview-connected-registry-access.md)
* [Deploy the connected registry Arc extension](https://learn.microsoft.com/azure/container-registry/quickstart-connected-registry-arc-cli)
* [Secure deployment options for the connected registry extension](https://learn.microsoft.com/azure/container-registry/tutorial-connected-registry-arc)
* [ABAC-enabled repository permissions in Azure Container Registry](https://learn.microsoft.com/azure/container-registry/container-registry-rbac-abac-repository-permissions)
* [Conditions for Azure role assignments](https://learn.microsoft.com/azure/role-based-access-control/conditions-overview)
* [Azure Arc-enabled Kubernetes quickstart](https://learn.microsoft.com/azure/azure-arc/kubernetes/quickstart-connect-cluster?tabs=azure-cli)
* [Workload identity on Arc-enabled Kubernetes - concepts](https://learn.microsoft.com/azure/azure-arc/kubernetes/conceptual-workload-identity)
* [Deploy and configure workload identity federation on Arc-enabled Kubernetes](https://learn.microsoft.com/azure/azure-arc/kubernetes/workload-identity)
* [Certificate Management for Azure Arc-enabled Kubernetes](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview)
* [Deploy Certificate Management for Azure Arc-enabled Kubernetes](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-deploy)
* [Managed identities for Azure resources](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview)
