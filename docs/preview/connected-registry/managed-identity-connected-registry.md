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

Azure Container Registry [connected registry](https://learn.microsoft.com/en-us/azure/container-registry/intro-connected-registry) is an on-premises or edge registry that synchronizes container images and OCI artifacts from a parent cloud registry. Historically, every connected registry authenticated with its parent using a **sync token** — an ACR scope map token whose password had to be generated, embedded in a connection string, distributed to the edge, and rotated manually.

With **managed identity authentication**, a connected registry running on an [Azure Arc-enabled Kubernetes](https://learn.microsoft.com/azure/azure-arc/kubernetes/overview) cluster authenticates to its parent registry using a **user-assigned managed identity** and [Azure Workload Identity](https://learn.microsoft.com/azure/azure-arc/kubernetes/conceptual-workload-identity). No long-lived secret is stored in the connection string, in a Kubernetes secret, or anywhere else in the deployment.

Managed identity authentication builds on [ABAC-enabled repository permissions](https://learn.microsoft.com/azure/container-registry/container-registry-rbac-abac-repository-permissions). Instead of a scope map, the set of repositories a connected registry synchronizes is derived from the Azure RBAC role assignment and ABAC conditions you grant to the managed identity on the parent registry.

Each connected registry has a **sync authentication mode**, set by the `authType` property when the connected registry is created. This article uses the following terms:

* **Sync token mode** (`authType` is `SyncToken`) — the existing behavior, where synchronization authenticates with an ACR scope map token.
* **Managed identity mode** (`authType` is `ManagedIdentity`) — the new behavior described in this article.

> [!IMPORTANT]
> Managed identity authentication requires the parent registry to be opted into **ABAC-enabled repository permissions mode**. Registries using legacy registry permissions are not supported. Before continuing, review [ABAC-enabled repository permissions](https://learn.microsoft.com/azure/container-registry/container-registry-rbac-abac-repository-permissions) and understand how opting a registry into ABAC changes the behavior of existing role assignments.

## What changes and what does not

| | Sync token mode (existing) | Managed identity mode (new) |
|---|---|---|
| Credential in the connection string | Token password | None — only a client ID |
| Credential rotation | Manual; requires redeploying the extension | Automatic; tokens are short-lived |
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

## Checklist for private preview - connected registry managed identity

Before you start, keep two rules in mind, because they shape everything else in this article:

* The managed identity's RBAC and ABAC permissions are the **only** definition of sync scope.
* Changing those permissions has **no effect on the edge** until you run `az acr connected-registry resync`.

The table below summarizes the steps you need to undertake to participate in the private preview. These steps are explained in detail later in this document.

| Step Number | Step Description |
|-------------|------------------|
| 1 | Register your subscription for the preview. This registration does not affect any Azure resources within the subscription. |
| 2 | Contact the ACR team at <acr-pm@microsoft.com> for approval of your subscription preview registration. |
| 3 | Prepare a **Premium** parent registry opted into **ABAC-enabled repository permissions** with the dedicated data endpoint enabled. |
| 4 | Create a user-assigned managed identity and assign it a connected registry sync role, scoped with ABAC conditions to the repositories you want to synchronize. |
| 5 | Connect your Kubernetes cluster to Azure Arc with the OIDC issuer and workload identity enabled, and align the Kubernetes API server service account issuer with the Arc OIDC issuer. |
| 6 | Create a federated identity credential binding the managed identity to the connected registry pod's service account. |
| 7 | Create the connected registry using the `2026-09-01-preview` REST API. The Azure CLI does not yet support the managed identity parameters. |
| 8 | Deploy the **Certificate Management for Azure Arc** extension, so that a single Microsoft-managed cert-manager controller issues and renews the connected registry's TLS certificates. |
| 9 | Deploy the preview build of the connected registry Arc extension, using a managed identity connection string that contains no password and setting `cert-manager.install=false`. |

## Prerequisites

* You can use the [Azure Cloud Shell](https://learn.microsoft.com/azure/cloud-shell/overview) or a local installation of the Azure CLI to run the command examples in this article. If you'd like to use it locally, run `az --version` to find your version. If you need to install or upgrade, see [Install Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli).
* The Azure CLI commands in this article are formatted for the Bash shell. If you're using a different shell like PowerShell or Command Prompt, you may need to adjust line continuation characters or variable assignment lines accordingly. This article uses variables to minimize the amount of command editing required.
* **Azure CLI 2.64.0 or later**, with the `connectedk8s` extension at **1.10.0 or later** and the `k8s-extension` extension installed. Earlier versions do not accept the `--enable-oidc-issuer` and `--enable-workload-identity` flags used in [Step 6](#step-6-connect-the-cluster-to-azure-arc-with-oidc-and-workload-identity).

    ```bash
    az version
    az extension add --upgrade --name connectedk8s
    az extension add --upgrade --name k8s-extension
    ```

* A **Premium** parent registry opted into ABAC-enabled repository permissions, with the dedicated data endpoint enabled.
* Permission to create managed identities, federated identity credentials, and **role assignments** on the registry (`Microsoft.Authorization/roleAssignments/write` — for example, the Owner or User Access Administrator role).
* A Kubernetes cluster running a [supported distribution](#supported-kubernetes-distributions), connected to Azure Arc, with the ability to restart the Kubernetes API server.
* A decision about certificate management. This article uses [Certificate Management for Azure Arc](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview) as the recommended TLS path, and only **one** cert-manager installation can exist on a cluster. If you already run open source cert-manager or trust-manager for other workloads, do not install a second one — read [Step 9](#step-9-install-certificate-management-for-azure-arc) before you begin. Certificate management is independent of the managed identity token exchange; it is the certificate direction ACR recommends for new Arc extension deployments.
* [kubectl](https://kubernetes.io/docs/tasks/tools/#kubectl) installed, with a `kubeconfig` file and context pointing to your cluster.
* [ORAS](https://oras.land/docs/installation) and [jq](https://jqlang.github.io/jq/) installed, to validate the deployment.
* Familiarity with the generally available deployment flow. This article assumes you have read [Deploy the connected registry Arc extension](https://learn.microsoft.com/azure/container-registry/quickstart-connected-registry-arc-cli) and describes only what differs under managed identity authentication. Where the two disagree — for example auto-upgrade, the connection string format, and `cert-manager.install` — follow this article.

### Supported configurations

| Requirement | Supported value |
|---|---|
| Parent registry SKU | **Premium** |
| Parent registry permissions mode | **ABAC-enabled repository permissions** (`AbacRepositoryPermissions`) |
| Registry dedicated data endpoint | **Enabled** |
| Connected registry topology | **Top-level only** — the parent must be the cloud registry |
| Nested connected registries | **Not supported** — a connected registry in managed identity mode cannot have children |
| Managed identity type | **User-assigned only**, exactly **one** identity |
| Edge platform | **Azure Arc-enabled Kubernetes** with the OIDC issuer and workload identity enabled |
| Connected registry Arc extension | **1.5.0 or later** |
| Connected registry mode | `ReadOnly` or `ReadWrite` |
| TLS certificate management | **[Certificate Management for Azure Arc](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview)** (recommended) — deploy the connected registry extension with `cert-manager.install=false` |
| API version | `2026-09-01-preview` |
| Azure cloud | Azure public cloud |

### Supported Kubernetes distributions

Managed identity authentication depends on Azure Arc workload identity, which supports only specific Kubernetes distributions. [Workload identity prerequisites](https://learn.microsoft.com/azure/azure-arc/kubernetes/workload-identity#prerequisites) is the authoritative, current list — always check it rather than the snapshot below. At the time of writing, the supported distributions include:

* Ubuntu Linux running **K3s**
* **AKS enabled by Azure Arc**
* **Red Hat OpenShift**
* **VMware Tanzu TKGm**
* **AKS on Edge Essentials**

> [!IMPORTANT]
> A generic Arc-connected Kubernetes cluster is **not** sufficient. If your distribution does not support Azure Arc workload identity, the workload identity webhook will not inject credentials into the connected registry pod and the deployment will fail with a crash loop. Clusters such as Kubernetes in Docker (kind), Minikube, and Docker Desktop are **not** supported for this feature.

## Subscription preview registration

To enable managed identity authentication for connected registry in private preview for your subscription, follow these steps:

1. **Submit subscription preview registration request.** Begin by submitting a request to register your subscription for the preview. Select the target subscription first, so that the registration does not land on your Azure CLI default subscription:

    ```bash
    az account set --subscription "<subscription-id>"

    az feature register \
    --namespace Microsoft.ContainerRegistry \
    --name ConnectedRegistryManagedIdentity
    ```

2. **Contact the ACR team.** After registering, you need to get registration approval from the ACR team at <acr-pm@microsoft.com>. Reach out to them with the details of your subscription preview registration request, including your subscription ID and intended region.

3. **Verify the registration state.** Do not continue until the feature state is `Registered`. This can take some time after approval.

    ```bash
    az feature show \
    --namespace Microsoft.ContainerRegistry \
    --name ConnectedRegistryManagedIdentity \
    --query properties.state --output tsv
    ```

4. **Propagate preview registration.** Once the feature `ConnectedRegistryManagedIdentity` is registered and approved, invoking `az provider register -n Microsoft.ContainerRegistry` is required to get the change propagated to the subscription.

    ```bash
    az provider register -n Microsoft.ContainerRegistry
    ```

## Set up the environment

The examples use Bash. Set these variables first.

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
NAMESPACE="connected-registry"
CR_MODE="ReadOnly"   # or ReadWrite

az account set --subscription "$SUBSCRIPTION_ID"
```

## Step 1: Prepare the parent registry

Create a Premium registry opted into ABAC-enabled repository permissions, or use an existing registry that already meets the requirements.

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

## Step 2: Create the user-assigned managed identity

```bash
az identity create --resource-group "$RG" --name "$UAMI" --location "$LOCATION"

UAMI_RESOURCE_ID="$(az identity show --resource-group "$RG" --name "$UAMI" --query id --output tsv)"
UAMI_CLIENT_ID="$(az identity show --resource-group "$RG" --name "$UAMI" --query clientId --output tsv)"
UAMI_PRINCIPAL_ID="$(az identity show --resource-group "$RG" --name "$UAMI" --query principalId --output tsv)"
```

> [!IMPORTANT]
> The identity binding on a connected registry is **immutable**. Create the identity you intend to keep. Changing or replacing the identity later requires recreating the connected registry.

## Step 3: Grant the managed identity permission on the parent registry

Assign one of the built-in connected registry sync roles to the managed identity, matching the connected registry's mode.

| Connected registry mode | Role |
|---|---|
| `ReadOnly` | `Container Registry Connected Registry Sync Reader` |
| `ReadWrite` | `Container Registry Connected Registry Sync Contributor` |

These roles grant the gateway actions required for activation and message exchange, plus repository content and metadata actions. The gateway actions are not repository-scoped; the repository actions are, so you narrow them with an ABAC condition.

> [!NOTE]
> Both built-in roles also include `Microsoft.ContainerRegistry/registries/catalog/read`. The connected registry uses it to list the parent registry's full catalog before probing which repositories it may read, so catalog listing must stay unrestricted for sync scope discovery to work. An ABAC condition scopes repository **content and metadata** access; it does not scope catalog listing, so repository *names* outside the condition remain enumerable by the identity.

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

> [!WARNING]
> Assign the sync role at **registry scope**, not at subscription or resource group scope. In managed identity mode, the identity's permissions are the only source of truth for the set of repositories that synchronize. A broader inherited role assignment silently widens the sync scope. Assigning a sync role **without** an ABAC condition grants permissions to all repositories in the registry.

> [!IMPORTANT]
> Azure RBAC changes can take **up to 10 minutes** to propagate. Deploying the connected registry extension before propagation completes causes activation to fail with `403`. Either wait before deploying, or be prepared to restart the pod. See [Troubleshooting](#troubleshooting).

## Step 4: Create a client token for local access

Managed identity covers synchronization only. Clients that pull from or push to the on-premises connected registry still need an ACR token.

```bash
CLIENT_PASSWORD="$(az acr token create \
  --name "$CLIENT_TOKEN_NAME" --registry "$ACR" \
  --repository hello content/read metadata/read \
  --repository app content/read metadata/read \
  --query credentials.passwords[0].value --output tsv)"

CLIENT_TOKEN_ID="$(az acr token show --name "$CLIENT_TOKEN_NAME" --registry "$ACR" --query id --output tsv)"
```

> [!NOTE]
> `clientTokenIds` controls which ACR tokens clients may use against the local connected registry. It does **not** grant the managed identity any synchronization permission. Synchronization permission comes solely from the role assignment created in Step 3.

> [!IMPORTANT]
> `CLIENT_PASSWORD` is a long-lived credential for local client access. Managed identity removes the *sync* secret, not this one. Do not enable shell tracing, echo it, write it to a file, or paste it into logs or issue reports. Pass it with `--password-stdin` as shown later, run `unset CLIENT_PASSWORD` when you finish, and regenerate the token with `az acr token credential generate` if it is ever exposed.

> [!NOTE]
> The token above deliberately grants local access to `app`, which the managed identity's ABAC condition **excludes** from synchronization. This is a negative test: it demonstrates that client token access and sync scope are independent. Pulling `app:cloud` from the edge is expected to return `404` until you widen the ABAC condition and run `az acr connected-registry resync`.

## Step 5: Create the connected registry

The Azure CLI does not yet expose the managed identity parameters, so create the connected registry with the ARM REST API using API version `2026-09-01-preview`. The request reuses the `CR_MODE` value you set in [Set up the environment](#set-up-the-environment), which must match the sync role assigned in Step 3.

```bash
cat > cr-body.json <<EOF
{
  "identity": {
    "type": "UserAssigned",
    "userAssignedIdentities": {
      "$UAMI_RESOURCE_ID": {}
    }
  },
  "properties": {
    "mode": "$CR_MODE",
    "parent": {
      "syncProperties": {
        "authType": "ManagedIdentity",
        "messageTtl": "P2D"
      }
    },
    "clientTokenIds": [
      "$CLIENT_TOKEN_ID"
    ],
    "logging": {
      "logLevel": "Information"
    }
  }
}
EOF

az rest --method put \
  --uri "https://management.azure.com${ACR_RESOURCE_ID}/connectedRegistries/${CR}?api-version=2026-09-01-preview" \
  --body @cr-body.json
```

Confirm that the response contains:

* `properties.parent.syncProperties.authType` set to `ManagedIdentity`
* your managed identity resource ID with resolved `clientId` and `principalId`
* **no** `properties.parent.syncProperties.tokenId`

Capture the gateway endpoint returned by the service:

```bash
PARENT_GATEWAY_ENDPOINT="$(az rest --method get \
  --uri "https://management.azure.com${ACR_RESOURCE_ID}/connectedRegistries/${CR}?api-version=2026-09-01-preview" \
  --query properties.parent.syncProperties.gatewayEndpoint --output tsv)"
```

> [!NOTE]
> Optional properties such as `parent.syncProperties.schedule`, `parent.syncProperties.syncWindow`, `garbageCollection`, and `notificationsList` behave the same as in sync token mode.

## Step 6: Connect the cluster to Azure Arc with OIDC and workload identity

Follow the [Azure Arc quickstart](https://learn.microsoft.com/azure/azure-arc/kubernetes/quickstart-connect-cluster?tabs=azure-cli) to prepare the cluster, then connect it with both flags:

```bash
az connectedk8s connect \
  --name "$ARC_CLUSTER" --resource-group "$RG" --location "$LOCATION" \
  --enable-oidc-issuer --enable-workload-identity
```

If the cluster is already connected to Azure Arc without these flags, enable them with:

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

> [!IMPORTANT]
> This step is required and easy to miss. Connecting the cluster with `--enable-oidc-issuer` and `--enable-workload-identity` creates the public Arc OIDC issuer and installs the workload identity webhook. It does **not** automatically change the service account issuer that your Kubernetes API server advertises. Until the two issuers match, Microsoft Entra ID rejects the projected token and the connected registry pod cannot authenticate.

Read the Arc OIDC issuer:

```bash
OIDC_ISSUER="$(az connectedk8s show --resource-group "$RG" --name "$ARC_CLUSTER" \
  --query oidcIssuerProfile.issuerUrl --output tsv)"
echo "$OIDC_ISSUER"
```

Confirm that `OIDC_ISSUER` is a public HTTPS URL before continuing.

Configure your API server to advertise this issuer. The exact procedure depends on your Kubernetes distribution — follow [configure workload identity settings on the Kubernetes cluster](https://learn.microsoft.com/azure/azure-arc/kubernetes/workload-identity#configure-workload-identity-settings-on-the-kubernetes-cluster), which covers each supported distribution.

Then verify the issuer advertised by the API server:

```bash
ADVERTISED_ISSUER="$(kubectl get --raw='/.well-known/openid-configuration' | jq -r '.issuer')"
printf 'Arc issuer:        %s\nAPI server issuer: %s\n' "$OIDC_ISSUER" "$ADVERTISED_ISSUER"
```

The two issuer values must match, allowing only a trailing-slash difference.

> [!WARNING]
> Do not create the federated identity credential or deploy the connected registry extension if the API server still reports `https://kubernetes.default.svc.cluster.local`. The deployment appears to succeed, but the connected registry never authenticates.

Confirm that the workload identity webhook is installed:

```bash
kubectl get mutatingwebhookconfiguration | grep -i workload-identity
```

## Step 8: Create the federated identity credential

The connected registry extension's Helm chart creates its workload identity service account deterministically as **`<extension-name>-wi-sa`** in the extension's namespace, where `<extension-name>` is the value you pass to `az k8s-extension create --name`. Because the federated identity credential must exist *before* the pod starts, decide the extension name and namespace now and use them consistently.

```bash
SERVICE_ACCOUNT="${EXTENSION_NAME}-wi-sa"
FIC_SUBJECT="system:serviceaccount:${NAMESPACE}:${SERVICE_ACCOUNT}"

az identity federated-credential create \
  --resource-group "$RG" --identity-name "$UAMI" --name "${CR}-fic" \
  --issuer "$OIDC_ISSUER" \
  --subject "$FIC_SUBJECT" \
  --audiences "api://AzureADTokenExchange"
```

> [!IMPORTANT]
> If you choose a different extension name or namespace, use those same values when building `FIC_SUBJECT` and when deploying the extension. A mismatch prevents the connected registry pod from obtaining a Microsoft Entra ID token.

## Step 9: Install Certificate Management for Azure Arc

The connected registry serves its endpoint over HTTPS, so it needs a TLS certificate. Historically the connected registry Arc extension installed its *own* bundled copy of open source cert-manager. Use the **[Certificate Management for Azure Arc](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview)** extension instead, and tell the connected registry extension not to install its own. It provides a single Microsoft-maintained certificate controller shared by every Arc extension on the cluster.

> [!NOTE]
> The ACR team intends to move all connected registry Arc extension customers to Certificate Management for Azure Arc. Starting on it now means you will not have to migrate your connected registry's certificate authority later.

> [!IMPORTANT]
> Only one cert-manager installation may exist on a cluster — Certificate Management for Azure Arc, the connected registry extension's bundled copy, and any open source cert-manager all register the same CRDs and webhooks. If you already run open source cert-manager or trust-manager, follow [Migrate from open source cert-manager and trust-manager](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-deploy#migrate-from-open-source-cert-manager-and-trust-manager) before continuing, and inventory the certificates those workloads depend on first.

Confirm that your cluster's region is in the [list of supported regions](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview#regional-support), then install the extension:

```bash
az k8s-extension create \
  --resource-group "$RG" \
  --cluster-name "$ARC_CLUSTER" \
  --cluster-type connectedClusters \
  --name "azure-cert-management" \
  --extension-type "microsoft.certmanagement"
```

Wait for it to become ready before continuing. The connected registry extension creates `cert-manager.io/v1` resources immediately, so deploying it too early fails intermittently:

```bash
kubectl wait --for=condition=Available --timeout=180s --all deployment -n cert-manager
```

You do not need to create an `Issuer` or `Certificate` resource yourself — the connected registry extension creates its own in the next step, and this controller issues and renews them.

For configuration options and troubleshooting, see the [Certificate Management for Azure Arc documentation](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-deploy).

## Step 10: Deploy the connected registry Arc extension

Build the managed identity connection string. It contains **no password**.

```bash
CONNECTION_STRING="ConnectedRegistryName=${CR};ManagedIdentityClientId=${UAMI_CLIENT_ID};ParentGatewayEndpoint=${PARENT_GATEWAY_ENDPOINT};ParentEndpointProtocol=https"
```

> [!IMPORTANT]
> Do not include `SyncTokenName` or `SyncTokenPassword` in a managed identity connection string. The two connection string formats are mutually exclusive, and supplying both is rejected when the connected registry starts.

Choose the cluster IP that the connected registry service will use, and export it. The address must fall inside the cluster's service IP range (service CIDR) and must not already be in use by another service:

```bash
CR_CLUSTER_IP="<cluster-ip-in-service-cidr>"

kubectl get services --all-namespaces --output wide
```

Deploy the extension:

```bash
az k8s-extension create \
  --name "$EXTENSION_NAME" \
  --cluster-name "$ARC_CLUSTER" --resource-group "$RG" \
  --cluster-type connectedClusters \
  --extension-type Microsoft.ContainerRegistry.ConnectedRegistry \
  --release-namespace "$NAMESPACE" \
  --auto-upgrade-minor-version true \
  --config service.clusterIP="$CR_CLUSTER_IP" \
  --config connectionString="$CONNECTION_STRING" \
  --config cert-manager.install=false
```

> [!IMPORTANT]
> `--release-namespace "$NAMESPACE"` is required. The Helm chart creates the workload identity service account in the release namespace, and the federated identity credential subject you created in [Step 8](#step-8-create-the-federated-identity-credential) is bound to `$NAMESPACE`. If the extension installs into a different namespace, token exchange fails with `AADSTS70021` even though the credential looks correct.

> [!NOTE]
> Managed identity support ships in connected registry Arc extension **1.5.0** and is carried forward in all later versions, so auto-upgrade is safe to leave on and is the recommended setting. Do not combine `--auto-upgrade-minor-version true` with `--version`; the Azure CLI rejects that combination with a mutually exclusive argument error. Pin `--version 1.5.0` only if you must hold a specific build, and in that case set `--auto-upgrade-minor-version false`.

> [!IMPORTANT]
> `cert-manager.install=false` is required when you use Certificate Management for Azure Arc. It is **not** the extension's default. If you omit it, the extension installs its own bundled cert-manager, which collides with the one installed in [Step 9](#step-9-install-certificate-management-for-azure-arc) and the deployment fails. This is the same configuration documented for a preinstalled cert-manager in [Secure deployment options for the connected registry extension](https://learn.microsoft.com/azure/container-registry/tutorial-connected-registry-arc).

The extension has two separate cert-manager settings, and the distinction matters:

| Setting | Default | Meaning |
|---|---|---|
| `cert-manager.enabled` | `true` | The extension creates cert-manager `Issuer` and `Certificate` resources to obtain its TLS certificate. Leave this `true`. |
| `cert-manager.install` | `true` | The extension also installs its own bundled copy of cert-manager. Set this to `false` so that Certificate Management for Azure Arc serves the request instead. |

> [!NOTE]
> Leave `cert-manager.enabled` at its default of `true`. Setting it to `false` means the extension no longer requests a certificate at all, and you must then supply your own certificate through `tls.secret`, or through `tls.crt` and `tls.key`. Setting `cert-manager.install=true` while `cert-manager.enabled=false` is rejected by the chart.

### Certificate subject names and the client endpoint

The server certificate that cert-manager issues always covers the in-cluster service DNS name, `<service-name>.<namespace>.svc.cluster.local`. Two settings add further subject alternative names, and both must be set at deployment time, before the certificate is issued:

| Setting | Effect |
|---|---|
| `service.clusterIP` | Pins the service to a specific cluster IP and adds that IP to the certificate's `ipAddresses` SAN. Set in the command above, because most clients reach the connected registry by IP. |
| `registryLoginServer` | Adds a custom DNS name to the certificate's `dnsNames` SAN. Add it when clients connect through a hostname you control. |

Clients that connect to an address not covered by one of these names must either skip verification — which you should do only for the validation below — or be reconfigured to use a covered name. Changing these settings later requires the certificate to be reissued.

### Trust distribution

By default the extension enables **trust distribution**, which pushes the connected registry's CA certificate to the container runtime trust store on the cluster's client nodes so that pods can pull over HTTPS without manual trust configuration. Managed identity authentication does not change this behavior, and neither does Certificate Management for Azure Arc.

Certificate handling and the identity token exchange are independent, so the alternatives documented in [Secure deployment options for the connected registry extension](https://learn.microsoft.com/azure/container-registry/tutorial-connected-registry-arc) — bring your own certificate, Kubernetes secret, and your own trust distribution — all remain available under managed identity authentication.

> [!IMPORTANT]
> Managed identity authentication requires connected registry Arc extension **1.5.0 or later**, which is published on the default `stable` release train. Deploying an earlier extension version does not enable managed identity authentication, and the connected registry falls back to expecting a sync token in its connection string.

## Validate the deployment

### Verify the Azure resource

```bash
az rest --method get \
  --uri "https://management.azure.com${ACR_RESOURCE_ID}/connectedRegistries/${CR}?api-version=2026-09-01-preview" \
  --query "{authType:properties.parent.syncProperties.authType, tokenId:properties.parent.syncProperties.tokenId, identity:identity, status:properties.status, connectionState:properties.connectionState}"
```

Expect `authType` to be `ManagedIdentity`, `tokenId` to be absent, and — once the edge deployment is healthy — `connectionState` to be `Online`.

### Verify workload identity injection

```bash
kubectl get serviceaccount "$SERVICE_ACCOUNT" --namespace "$NAMESPACE" --output yaml
```

The service account must carry the annotation `azure.workload.identity/client-id` set to your managed identity's client ID.

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

The connected registry uses a `ClusterIP` service. Run the following commands on a cluster node to discover its endpoint:

```bash
CR_SERVICE="$(kubectl --namespace "$NAMESPACE" get services \
  --output jsonpath='{range .items[*]}{.metadata.name}{"\n"}{end}' | grep -v cert-manager | head -1)"
CR_SERVICE_IP="$(kubectl --namespace "$NAMESPACE" get service "$CR_SERVICE" --output jsonpath='{.spec.clusterIP}')"
CR_SERVICE_PORT="$(kubectl --namespace "$NAMESPACE" get service "$CR_SERVICE" --output jsonpath='{.spec.ports[0].port}')"
CR_ENDPOINT="${CR_SERVICE_IP}:${CR_SERVICE_PORT}"
```

Log in with the client token created in Step 4 and compare the local manifest digest with the parent registry:

> [!WARNING]
> `--insecure` skips TLS verification. It is used here only to keep the smoke test independent of node trust configuration, so that an identity or synchronization problem is not masked by a certificate trust problem. Do not use `--insecure` for normal pulls. Because you set `service.clusterIP`, the certificate does cover this IP address — for verified access, drop `--insecure` once the node trusts the connected registry's CA, which trust distribution configures automatically for the cluster's container runtime.

```bash
printf '%s' "$CLIENT_PASSWORD" | oras login "$CR_ENDPOINT" \
  --username "$CLIENT_TOKEN_NAME" --password-stdin --insecure

CLOUD_DIGEST="$(az acr repository show --name "$ACR" --image "hello:cloud" --query digest --output tsv)"
LOCAL_DIGEST="$(oras manifest fetch "$CR_ENDPOINT/hello:cloud" --insecure --descriptor | jq -r '.digest')"
printf 'Cloud digest: %s\nLocal digest: %s\n' "$CLOUD_DIGEST" "$LOCAL_DIGEST"
test "$CLOUD_DIGEST" = "$LOCAL_DIGEST" && echo "PASS: client pull succeeded" || echo "FAIL: digest mismatch"
```

A `401` response indicates a client token, password, repository scope, or `clientTokenIds` problem. A `404` response usually means that authentication succeeded but the artifact has not synchronized yet, or is excluded by the managed identity's ABAC condition. Pulling `app:cloud` is expected to return `404`, because the ABAC condition deliberately excludes it.

When you finish validating, clear the client credential from your shell:

```bash
unset CLIENT_PASSWORD
```

## Understanding sync scope in managed identity mode

In sync token mode, the scope map defines which repositories synchronize. **In managed identity mode, the identity's RBAC and ABAC permissions are the only source of truth for sync scope.** There is no separate repository list and no `--repository` parameter.

During a full synchronization, the connected registry lists the parent registry's catalog, probes which repositories the managed identity is permitted to read, and synchronizes only those.

This design has several important consequences:

* **Permission changes do not take effect automatically.** After modifying a role assignment or an ABAC condition, you must trigger a resync so the connected registry re-discovers its scope:

    ```bash
    az acr connected-registry resync --registry "$ACR" --name "$CR"
    ```

* **Inherited role assignments widen the sync scope silently.** A role assignment at subscription or resource group scope also applies to the registry. Always assign at registry scope with an ABAC condition, and audit for inherited assignments:

    ```bash
    az role assignment list --assignee "$UAMI_PRINCIPAL_ID" --all --include-inherited --output table
    ```

* **Narrowing permissions takes effect on the edge only after a resync.** Removing a repository from the ABAC condition stops future synchronization immediately, but artifacts already present on the edge remain available until a full resync runs. During a resync, the connected registry rebuilds the permitted catalog from the identity's *current* permissions and removes local repositories that are no longer in it. Run `az acr connected-registry resync` after narrowing permissions, and do not treat the narrowing alone as having revoked access to already-synchronized content.

* **There is no API that reports the effective sync scope.** To understand which repositories will synchronize, inspect the managed identity's role assignments and conditions.

* **RBAC propagation can take up to 10 minutes.** Expect a delay between granting access and seeing repositories appear on the edge.

## Troubleshooting

The primary diagnostic surface for connected registries in managed identity mode is the pod log on the Arc-enabled Kubernetes cluster:

```bash
kubectl logs --namespace "$NAMESPACE" "$POD"
kubectl logs --namespace "$NAMESPACE" "$POD" --previous   # for a crashed container
```

### Symptom matrix

| Symptom | Likely cause | Resolution |
|---|---|---|
| Pod in `CrashLoopBackOff`; log reports that managed identity configuration is incomplete and that `AZURE_TENANT_ID` and `AZURE_FEDERATED_TOKEN_FILE` are required | Workload identity was never injected. The OIDC issuer or workload identity is not enabled on the Arc cluster, or the distribution is unsupported | [Recovery A](#recovery-a-workload-identity-not-enabled) |
| Log reports that the federated token file does not exist | The webhook added the environment variables but the projected volume is missing, or the pod predates webhook installation | Delete the pod so it is recreated through the admission webhook |
| Log shows `AADSTS70021: No matching federated identity record found for presented assertion` | The federated identity credential's issuer or subject does not match the cluster | [Recovery B](#recovery-b-federated-identity-credential-mismatch) |
| The API server reports its issuer as `https://kubernetes.default.svc.cluster.local` | The API server service account issuer was never aligned to the Arc OIDC issuer | Repeat [Step 7](#step-7-align-the-kubernetes-api-server-service-account-issuer), then restart the pod |
| Activation fails with HTTP 403; the startup log reports `status: Forbidden` and that the connected registry instance failed to activate. The pod terminates and restarts | The sync role assignment is missing, or has not finished propagating | Verify the assignment, wait up to 10 minutes, then delete the pod |
| Pod is healthy but no repositories synchronize | The ABAC condition excludes every repository, or the role grants gateway actions only | Review and correct the condition, then run `az acr connected-registry resync` |
| Some repositories synchronize and others do not | The ABAC condition does not match those repository names | Update the condition, then resync |
| Synchronization worked and then stopped | The role assignment was removed or narrowed, or the Arc OIDC issuer changed | If the narrowing was accidental, restore permissions and resync. If it was intentional, resync so the edge catalog is rebuilt from the current permissions. If the cluster was rebuilt, see [Recovery C](#recovery-c-oidc-issuer-rotation) |
| Synchronization resumed after a permissions fix, but some artifacts pushed during the outage are still missing | Individual push, delete, and tag events that failed with `403` are classified as non-retryable and are dropped. Restarting the pod does not replay them | Run `az acr connected-registry resync --registry "$ACR" --name "$CR"` to reconcile the full catalog |
| Connected registry creation fails with an error stating that managed identity sync is only supported on ABAC-enabled registries | The parent registry uses legacy registry permissions | Opt the registry into ABAC-enabled repository permissions |
| Connected registry creation fails because the feature is not registered | The `ConnectedRegistryManagedIdentity` feature is not registered on the subscription | Complete [Subscription preview registration](#subscription-preview-registration) |
| ARM rejects the request with a `400` on the `identity` property | The managed identity does not exist, belongs to a different tenant, is system-assigned, or more than one identity was supplied | Supply exactly one existing user-assigned managed identity |
| Extension installation fails on cert-manager CRDs or webhooks, reporting that a resource already exists or is owned by another release | Two cert-manager installations collide, because the extension installed its bundled copy alongside Certificate Management for Azure Arc | Delete the extension, then redeploy it with `--config cert-manager.install=false`. See [Step 10](#step-10-deploy-the-connected-registry-arc-extension) |
| The connected registry pod stays `Pending` or `0/1 Ready`, and its TLS secret stays empty | The extension created `Certificate` resources, but no cert-manager is present to issue them, because `cert-manager.install=false` was set without installing Certificate Management for Azure Arc | Install Certificate Management for Azure Arc. See [Step 9](#step-9-install-certificate-management-for-azure-arc). Then inspect the request with `kubectl describe certificate -n $NAMESPACE` |

### Recovery A: Workload identity not enabled

If you deployed the extension before enabling the OIDC issuer and workload identity, the pod never received credentials. **In most cases you do not need to delete the extension.** Credential injection happens at pod admission time, so recreating the pod after fixing the cluster is sufficient.

1. Enable the OIDC issuer and workload identity on the Arc cluster. See [Step 6](#step-6-connect-the-cluster-to-azure-arc-with-oidc-and-workload-identity).
2. Align the API server service account issuer and verify that the issuers match. See [Step 7](#step-7-align-the-kubernetes-api-server-service-account-issuer).
3. Confirm that the workload identity webhook exists:

    ```bash
    kubectl get mutatingwebhookconfiguration | grep -i workload-identity
    ```

4. Create the federated identity credential. See [Step 8](#step-8-create-the-federated-identity-credential).
5. Recreate the pod so the webhook can inject credentials:

    ```bash
    kubectl rollout restart deployment "$EXTENSION_NAME" --namespace "$NAMESPACE"
    ```

6. Re-run the [workload identity verification](#verify-workload-identity-injection).

Delete and recreate the extension **only if** the extension name, namespace, connection string, or managed identity client ID is wrong, or the extension is stuck in a failed provisioning state:

> [!WARNING]
> Deleting the extension uninstalls the connected registry and interrupts local registry service for every client on the cluster. It also removes the chart-managed resources, including the persistent volume claim holding synchronized artifacts and the issued TLS certificates. Confirm that nothing needs to be retained before you run this, and prefer restarting the deployment or updating the extension whenever the release is recoverable.

```bash
az k8s-extension delete --name "$EXTENSION_NAME" --cluster-name "$ARC_CLUSTER" \
  --resource-group "$RG" --cluster-type connectedClusters --yes
```

Then correct the configuration and repeat [Step 10](#step-10-deploy-the-connected-registry-arc-extension).

### Recovery B: Federated identity credential mismatch

Compare the credential against the cluster's actual values:

```bash
az identity federated-credential list --resource-group "$RG" --identity-name "$UAMI" \
  --query "[].{name:name, issuer:issuer, subject:subject, audiences:audiences}" --output table

kubectl get serviceaccount --namespace "$NAMESPACE"

az connectedk8s show --resource-group "$RG" --name "$ARC_CLUSTER" \
  --query oidcIssuerProfile.issuerUrl --output tsv
```

The `subject` must be exactly `system:serviceaccount:<namespace>:<extension-name>-wi-sa`, the `issuer` must equal the Arc OIDC issuer, and `audiences` must contain `api://AzureADTokenExchange`. Update or recreate the credential, then restart the pod.

### Recovery C: OIDC issuer rotation

If the Arc cluster's OIDC issuer URL changes — for example, after a cluster rebuild or migration — the existing federated identity credential becomes invalid and token acquisition fails.

1. Read the new issuer URL from the Arc resource.
2. Delete and recreate the federated identity credential with the new issuer.
3. Restart the connected registry pod.

### Collect more diagnostic detail

To collect more detail, update the connected registry resource with debug logging:

```json
"logging": { "logLevel": "Debug" }
```

Access tokens are never written to logs. When sharing logs for support, still redact connection strings, tokens, and passwords.

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

1. Complete Steps 2, 3, 6, 7, and 8 of this article: create the managed identity, assign the sync role with an ABAC condition, enable the OIDC issuer and workload identity, align the API server service account issuer, and create the federated identity credential.

2. Decide your certificate authority before touching the extension. If the existing deployment uses the extension's bundled cert-manager, you must either keep using it — by omitting `cert-manager.install=false` in step 5 — or migrate to Certificate Management for Azure Arc by completing [Step 9](#step-9-install-certificate-management-for-azure-arc) first. Running both at once collides on the cert-manager CRDs and webhooks.

3. Deactivate the connected registry and confirm that it is offline:

    ```bash
    az acr connected-registry deactivate --registry "$ACR" --name "$CR" --yes

    az rest --method get \
      --uri "https://management.azure.com${ACR_RESOURCE_ID}/connectedRegistries/${CR}?api-version=2026-09-01-preview" \
      --query "{status:properties.status, connectionState:properties.connectionState}"
    ```

    Do not continue until the connected registry reports that it is offline. Patching the authentication mode while it is still online is rejected.

4. Update the connected registry to managed identity authentication:

    ```bash
    cat > cr-migrate.json <<EOF
    {
      "identity": {
        "type": "UserAssigned",
        "userAssignedIdentities": { "$UAMI_RESOURCE_ID": {} }
      },
      "properties": {
        "parent": { "syncProperties": { "authType": "ManagedIdentity" } }
      }
    }
    EOF

    az rest --method patch \
      --uri "https://management.azure.com${ACR_RESOURCE_ID}/connectedRegistries/${CR}?api-version=2026-09-01-preview" \
      --body @cr-migrate.json
    ```

5. Rebuild the connection string in managed identity format, as shown in [Step 10](#step-10-deploy-the-connected-registry-arc-extension), and update the existing Arc extension in place so the edge deployment uses the new credentials:

    ```bash
    CONNECTION_STRING="ConnectedRegistryName=${CR};ManagedIdentityClientId=${UAMI_CLIENT_ID};ParentGatewayEndpoint=${PARENT_GATEWAY_ENDPOINT};ParentEndpointProtocol=https"

    az k8s-extension update \
      --name "$EXTENSION_NAME" \
      --cluster-name "$ARC_CLUSTER" --resource-group "$RG" \
      --cluster-type connectedClusters \
      --config connectionString="$CONNECTION_STRING"
    ```

    Add `--config cert-manager.install=false` only if you migrated to Certificate Management for Azure Arc in step 2. If the extension cannot be updated in place — for example because its release namespace is wrong for the federated identity credential subject — delete and recreate it instead, observing the warning in [Recovery A](#recovery-a-workload-identity-not-enabled).

6. Validate the deployment. See [Validate the deployment](#validate-the-deployment).

> [!IMPORTANT]
> The cloud resource and the deployed Arc extension must use the same authentication mode. Update both, or the connected registry will not reconnect.

## Private preview limitations of managed identity authentication

During the private preview of managed identity authentication for connected registry, there are several limitations that you should be aware of.

### Feature limitations

1. **ABAC-enabled registries only.** Managed identity requires a parent registry opted into ABAC-enabled repository permissions. Registries using legacy registry permissions are not supported.
2. **Top-level connected registries only.** A connected registry in managed identity mode cannot have child connected registries, and a connected registry whose parent is another connected registry must remain in sync token mode.
3. **Exactly one user-assigned managed identity.** System-assigned managed identities are not supported.
4. **The identity binding is immutable** after the connected registry is created.
5. **Migration is one-way**, from sync token mode to managed identity mode only.
6. **Sync scope is derived from RBAC and ABAC permissions.** There is no explicit repository filter field, and no API reports the effective sync scope.
7. **Permission changes require a manual resync.** Role assignment and ABAC condition changes are not detected automatically.

### Tooling limitations

1. **Azure CLI does not yet support the managed identity properties** for `az acr connected-registry create`, `update`, or `get-settings`. Use the REST API as shown in this article. Other commands, including `deactivate`, `resync`, `show`, and `list`, work normally.
2. **`az acr connected-registry permissions` applies only to connected registries in sync token mode**, because its output is derived from a scope map.
3. **The Azure portal can display a connected registry in managed identity mode, but cannot create one** or perform the migration. Use the REST API.
4. **Older API versions omit managed identity properties.** Reading a connected registry in managed identity mode with an older API version omits `identity`, `authType`, and `tokenId`, because those versions cannot represent managed identity authentication. Use `2026-09-01-preview` to see the full resource.
5. **Connected registry Arc extension 1.5.0 or later is required.** Earlier extension versions do not include managed identity support.

### Operational notes

1. **Azure RBAC propagation can take up to 10 minutes.**
2. **Activation failures return `403` and are non-retryable.** The pod terminates and Kubernetes restarts it. This is expected behavior while permissions propagate.
3. If the managed identity loses gateway permissions after activation, synchronization stalls silently, and recovery begins only after `messageTtl` elapses.
4. Full synchronization lists the parent registry's entire catalog and then probes repository permissions in batches, so synchronization initiation cost grows roughly as the catalog listing plus one token request per batch of repositories. A parent registry with far more repositories than the connected registry is permitted to read still pays the full catalog cost on every resync.
5. **Certificate Management for Azure Arc is the recommended certificate authority for new deployments.** The ACR team plans to move all connected registry Arc extension customers to it, so adopting it now avoids a later migration of your cluster's certificate authority. Its [regional availability](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview#regional-support) and [validated distributions](https://learn.microsoft.com/azure/azure-arc/kubernetes/cert-manager-overview#validated-arc-enabled-kubernetes-distributions) are narrower than Azure Arc as a whole, so confirm your cluster qualifies before you plan a deployment.

## Reporting issues and asking for help

For issues with this preview feature, the fastest path to a resolution is to contact the ACR team directly, because the engineers who own this feature triage these reports.

To report issues, [create a new bug](https://github.com/Azure/acr/issues/new?assignees=&labels=connected-registry,bug&template=bug_report.md&title=) in this repository, or contact the ACR team at <acr-pm@microsoft.com>.

If an issue affects your **parent registry** — for example synchronization failures, throttling, or anything impacting production traffic — open an Azure support case as you normally would, and mention that the connected registry is enrolled in the managed identity mode private preview.

When reporting a problem, please include:

* Subscription ID, registry name, connected registry name, and region
* The UTC timestamp of the failure, and any correlation ID from the ARM response
* The connected registry extension version and release train
* Kubernetes distribution and version, and the Azure Arc agent version
* Relevant pod logs, **with connection strings, tokens, and passwords redacted**
* What you expected to happen, and what happened instead

We are especially interested in feedback on:

* The RBAC and ABAC-derived sync scope model, and whether an explicit repository filter would serve you better
* The manual resync requirement after permission changes
* Setup friction, particularly the API server service account issuer alignment step
* Which Kubernetes distributions you need supported

## Conclusion

Managed identity authentication removes the last long-lived secret from the connected registry synchronization path. Instead of generating, distributing, and rotating sync token passwords, you grant a user-assigned managed identity a connected registry sync role scoped with ABAC conditions, and the edge workload acquires short-lived Microsoft Entra ID tokens through Azure Arc workload identity. This aligns connected registry access control with the rest of Azure, lets you reuse the same repository scoping model you already use for ABAC-enabled registries, and eliminates an entire class of credential management and rotation work.

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
