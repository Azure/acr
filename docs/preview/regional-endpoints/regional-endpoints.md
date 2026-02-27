---
title: Regional endpoints for geo-replicated registries (Preview)
description: Learn how to use regional endpoints to target specific geo-replicas in Azure Container Registry for predictable routing and client-side failover.
ms.topic: how-to
ms.date: "2026-03-02"
ms.author: johsh
ms.service: azure-container-registry
---

## Regional endpoints for geo-replicated registries (Preview)

Azure Container Registry regional endpoints allow you to target specific geo-replicas directly, bypassing Azure-managed routing. This feature is useful when you need predictable routing, client-side failover, or regional affinity for your container registry operations.

> [!IMPORTANT]
> Regional endpoints are currently in **private preview**. To enable the preview, see [Enroll in the preview](#enroll-in-the-preview).

## About regional endpoints

When you use a geo-replicated registry's global endpoint (`myregistry.azurecr.io`), Azure automatically routes requests to the most suitable replica based on network performance. While this works well for most scenarios, it doesn't provide explicit control over which replica handles your requests.

Regional endpoints solve this by providing dedicated login server URLs for each geo-replica.

> [!IMPORTANT]
> **Clarification: `--regional-endpoints` vs `--region-endpoint-enabled`**
>
> These two settings have similar names but serve different purposes:
>
> | Setting | Scope | Purpose |
> |---------|-------|---------|
> | `--regional-endpoints` | Registry-level | Enables dedicated regional endpoint URLs (`myregistry.<region>.geo.azurecr.io`) for all geo-replicas. This is the feature documented on this page. |
> | `--region-endpoint-enabled` | Per-geo-replica | Controls whether the **global endpoint** (`myregistry.azurecr.io`) routes traffic to a specific geo-replica. Set to `false` to temporarily exclude a geo-replica from global endpoint routing (for maintenance or troubleshooting). Data continues syncing regardless of this setting. See [Geo-replication in Azure Container Registry](https://learn.microsoft.com/azure/container-registry/container-registry-geo-replication). |
>
> **These settings are independent.** Setting `--region-endpoint-enabled false` on a geo-replica:
> - Excludes that geo-replica from **global endpoint** routing only.
> - Does **not** disable the geo-replica's **regional endpoint** URL. If `--regional-endpoints` is enabled at the registry level, clients can still directly access that geo-replica via the regional endpoint URL.
> - Does **not** stop data syncing to that geo-replica.
>
> **In short:**
> - Enable `--regional-endpoints` at the registry level to **enable dedicated regional URLs** (for all geo-replicas) for direct access to specific geo-replicas.
> - Configure `--region-endpoint-enabled` (on a specific geo-replica) to **control global endpoint routing** to a specific geo-replica.

Regional endpoints provide dedicated login server URLs for each geo-replica:

```
myregistry.<region-name>.geo.azurecr.io
```

For example:

- `myregistry.eastus.geo.azurecr.io`
- `myregistry.westeurope.geo.azurecr.io`

### When to use regional endpoints

| Scenario | Description |
|----------|-------------|
| **Client-side failover** | Implement your own failover logic that explicitly switches between regions based on health checks. |
| **Regional affinity** | Ensure specific applications always use a designated replica. |
| **Troubleshooting** | Test or debug a specific regional replica. |
| **Push/pull consistency** | Ensure images are pushed and pulled from the same replica. |

### Regional endpoints coexist with global endpoints

Enabling regional endpoints doesn't disable or replace the global endpoint. You can use both simultaneously:

- Use the **global endpoint** (`myregistry.azurecr.io`) for most operations with automatic routing.
- Use **regional endpoints** when you need explicit regional control.

## Prerequisites

- **Premium SKU** - Regional endpoints are available exclusively on Premium tier registries.
- **Azure CLI** - Version 2.74.0 or later.
- **Preview feature registration** - You must register the `RegionalEndpoints` feature flag. See [Enroll in the preview](#enroll-in-the-preview).
- **API version** - Regional endpoints are available in all production regions in Azure Public Cloud via the `2026-01-01-preview` ACR ARM API version.

> [!NOTE]
> During private preview, regional endpoints are only available in Azure Public Cloud. Support for Azure Government, Azure China, and other national clouds will be available in public preview and beyond.

> [!NOTE]
> Regional endpoints can be enabled on any Premium SKU registry, even without geo-replication. A registry without geo-replication has a single geo-replica in the home region, which gets one regional endpoint URL. However, the feature is most useful when your registry has at least two geo-replicas.

## Enroll in the preview

To enable the regional endpoints private preview, complete the following steps before using regional endpoints.

### 1. Register the feature flag

Register the `RegionalEndpoints` feature flag for your subscription:

```azurecli
az feature register \
  --namespace Microsoft.ContainerRegistry \
  --name RegionalEndpoints
```

The feature registration is auto-approved and takes approximately 1 hour to propagate. You can check the status with:

```azurecli
az feature show \
  --namespace Microsoft.ContainerRegistry \
  --name RegionalEndpoints
```

Wait until the `state` shows **Registered** before proceeding.

### 2. Propagate the registration

Once the feature registration has propagated, update your provider registration:

```azurecli
az provider register -n Microsoft.ContainerRegistry
```

### 3. Install the preview CLI extension

Install the preview Azure CLI extension for regional endpoints:

Download the preview Azure CLI extension wheel file from <https://aka.ms/acr/regionalendpoints/download> and install it:

```azurecli
# Download the .whl file from the link above, then install:
az extension add \
  --source acrregionalendpoint-1.0.0b1-py3-none-any.whl \
  --allow-preview true
```

## Enable regional endpoints

You can enable regional endpoints when creating a new registry or update an existing registry.

**Create a new registry with regional endpoints enabled for all geo-replicas:**

```azurecli
az acr create \
  -n myregistry \
  -g myrg \
  -l regionname \
  --sku Premium \
  --regional-endpoints enabled
```

**Enable regional endpoints for all geo-replicas for an existing registry:**

```azurecli
az acr update \
  -n myregistry \
  -g myrg \
  --regional-endpoints enabled
```

---

Regional endpoints are enabled at the registry level and apply to every geo-replica. You can't enable regional endpoints for individual replicas. When you enable regional endpoints, Azure Container Registry automatically creates login server URLs for each of your geo-replicas.

### View all endpoints

Use the `az acr show-endpoints` command to view all endpoints for your registry, including the global URL, regional endpoints (if enabled), and dedicated data endpoints (if enabled):

```azurecli
az acr show-endpoints --name myregistry --resource-group myrg
```

This command displays:

- The global login server URL (`myregistry.azurecr.io`)
- Regional endpoint URLs for each geo-replica (if regional endpoints are enabled)
- Dedicated data endpoint URLs for each geo-replica (if dedicated data endpoints are enabled)

## Authenticate and use regional endpoints

Regional endpoints support the same authentication methods as the global endpoint: Microsoft Entra ID (formerly Azure Active Directory), service principals, managed identities, and admin credentials.

### Sign in to a regional endpoint

**Sign in to the global endpoint (default):**

```azurecli
az acr login --name myregistry
```

**Sign in to a specific regional endpoint:**

```azurecli
az acr login --name myregistry --endpoint eastus
```

### Tag and push an image to a regional endpoint

Tag an existing image with the regional endpoint URL, then push it:

```bash
docker tag myapp:v1 myregistry.eastus.geo.azurecr.io/myapp:v1
docker push myregistry.eastus.geo.azurecr.io/myapp:v1
```

### Pull an image from a regional endpoint

```bash
docker pull myregistry.eastus.geo.azurecr.io/myapp:v1
```

## Use regional endpoints with Kubernetes

You can specify regional endpoints directly in Kubernetes deployment manifests. This ensures clusters in specific regions always pull from their local replica.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  template:
    spec:
      containers:
      - name: myapp
        image: myregistry.eastus.geo.azurecr.io/myapp:v1
```

For information about authenticating Azure Kubernetes Service (AKS) with ACR, see [Authenticate with Azure Container Registry from Azure Kubernetes Service](https://learn.microsoft.com/azure/container-registry/container-registry-auth-aks).

## Import from specific geo-replicas

When importing images between registries, you can use regional endpoints to import from a specific geo-replica of the source registry. This is useful for scenarios where you want predictable network paths or need to import from a replica in a specific region.

**Import from the global endpoint (Azure chooses the replica):**

```azurecli
az acr import \
  --name mydownstreamregistry \
  --source myupstreamregistry.azurecr.io/myapp:v1 \
  --image myapp:v1
```

**Import from a specific geo-replica using its regional endpoint:**

```azurecli
az acr import \
  --name mydownstreamregistry \
  --source myupstreamregistry.westeurope.geo.azurecr.io/myapp:v1 \
  --image myapp:v1
```

This allows downstream registries to explicitly import from a specific geo-replica of an upstream registry, providing control over which regional replica serves the import operation.

## Network considerations

### Firewall rules

When using regional endpoints, configure your firewall rules to allow access to:

| Endpoint | Purpose |
|----------|---------|
| `myregistry.<region-name>.geo.azurecr.io` | Regional endpoint for registry operations |
| `myregistry.azurecr.io` | Global endpoint (if also used) |
| `myregistry.<region-name>.data.azurecr.io` | Layer downloads (if using private endpoints or dedicated data endpoints) |
| `*.blob.core.windows.net` | Layer downloads (if not using private endpoints or dedicated data endpoints) |

### Private endpoints

For registries with private endpoints enabled, enabling regional endpoints creates an additional private IP address for each geo-replica in all associated virtual networks.

**Example**: If your registry has 3 geo-replicas and you enable regional endpoints, each virtual network with a private endpoint to your registry consumes 3 additional private IP addresses (one per regional endpoint).

For more information, see [Connect privately to an Azure container registry using Azure Private Link](https://learn.microsoft.com/azure/container-registry/container-registry-private-link).

### Dedicated data endpoints

Regional endpoints work with [dedicated data endpoints](https://learn.microsoft.com/azure/container-registry/container-registry-dedicated-data-endpoints). When both features are enabled, layer downloads from regional endpoints automatically redirect to the geo-replica's dedicated data endpoint.

> [!TIP]
> It is recommended to also enable dedicated data endpoints for optimal in-region performance when using regional endpoints:
>
> ```azurecli
> az acr update -n <registry-name> --data-endpoint-enabled true
> ```

## Endpoint types reference

| Endpoint type | URL format | Purpose |
|---------------|------------|---------|
| Global endpoint | `myregistry.azurecr.io` | Login server with Azure-managed routing to any geo-replica |
| Regional endpoint | `myregistry.<region-name>.geo.azurecr.io` | Login server for a specific geo-replica |
| Data endpoint | `myregistry.<region-name>.data.azurecr.io` | Layer downloads for private endpoint or dedicated data endpoint-enabled registries |

## Related content

- [Geo-replication in Azure Container Registry](https://learn.microsoft.com/azure/container-registry/container-registry-geo-replication)
- [Dedicated data endpoints for Azure Container Registry](https://learn.microsoft.com/azure/container-registry/container-registry-dedicated-data-endpoints)
- [Connect privately using Azure Private Link](https://learn.microsoft.com/azure/container-registry/container-registry-private-link)
- [Configure firewall access rules](https://learn.microsoft.com/azure/container-registry/container-registry-firewall-access-rules)
