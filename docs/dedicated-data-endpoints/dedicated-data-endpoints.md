# Azure Container Registry: Dedicated Data Endpoints – Mitigating Data Exfiltration

Azure Container Registry announces dedicated data-endpoints, enabling tightly scoped client firewall rules to specific registries, minimizing data exfiltration concerns.

When users pull content from a registry, they primarily think of the registry url: `docker pull contoso.azurecr.io`. What most users do not realize is the registry URL is a REST endpoint for authentication and content discovery. Once a client initiates a pull requests, identifying the layers required, a secondary data-endpoint is serves blobs representing content layers.

<img src="./media/registry-dual-endpoints.png" width=170x>

## Registry Managed Storage Accounts

Azure Container Registry is a multi-tenant service, where the data-endpoint storage accounts are managed by the registry service. There are many benefits for managed storage, such as load balancing, contentious content splitting, multiple copies for higher concurrent content delivery, and multi-region support with [geo-replication](https://aka.ms/acr/geo-replicatin). However, to account for the multiple managed storage accounts, a wildcard must be used, representing the storage account name of the data-endpoint.

<img src="./media/registry-client-rules-all-storage.png" width=500x>

## Azure Private Link VNet Support
Azure Container Registry recently announced [Private Link support](https://aka.ms/acr/privatelink), enabling a private endpoint from Azure VNets to be placed on the managed registry service. In this case, both the registry and data-endpoints are accessible from within the VNet. The public endpoint can then be removed, securing the managed registry and storage accounts to access from within the VNet.

<img src="./media/registry-private-link.png" width=500x>

Unfortunately, VNet connectivity isn’t always an option.

## Client Firewall Rules & Data Exfiltration Risks

When connecting to a registry from on-prem hosts, IoT devices, custom build agents, or when Private Link simply is not an option, a client firewall rule could be applied, limiting access to specific resources.

As customers lock down their clients and VNets, the wildcard opens a data exfiltration risk as rouge code running in the secured environment could write secure data out to other “bad-dev” storage accounts.

<img src="./media/registry-data-exfiltration.png" width=500x>

## Dedicated Data-endpoints

Content is proxied through the registry service. Dedicated data-endpoints use the same proxy without requiring Private Links, providing known FQDNs for client firewall rules, avoiding the need for wildcards which blocks data exfiltration concerns. which blocks data exfiltration concerns.

<img src="./media/registry-dedicated-data-endpoint.png" width=500x>

## Enabling Dedicated Data-endpoints

Switching to dedicated data-endpoints will impact clients that have configured firewall access to the existing `*.blob.core.windows.net` endpoints, causing pull failures. To assure clients have consistent access, add the new data-endpoints to the client firewall rules. Once completed, existing registries can enable dedicated data-endpoints through the az cli, or the Azure portal.

### Private Preview Configuration

Until the Portal and az cli are enabled, customers can use the `az rest` api to enable dedicated data-endpoints.

- Set the registry default

  ```sh
  az configure --defaults acr=demo42
  ```

- Export the resource id of the registry

  ```sh
  export RESOURCE_ID=$(az acr show --query id -o tsv)
  ```

- Execute the REST api with the `az rest` command

  ```sh
  az rest --method patch --uri "$RESOURCE_ID?api-version=2019-12-01-preview" --body "{ \"properties\":{\"dataEndpointEnabled\":true}}" -o json
  ```

  Look for:

  ```json
  "dataEndpointEnabled": true,
      "dataEndpointHostNames": [
        "demo42.eastus.data.azurecr.io"
  ```

### az CLI

Using [az cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) version 2.3.1 or greater, run the [az acr update](https://docs.microsoft.com/en-us/cli/azure/acr?view=azure-cli-latest#az-acr-update) command:

```sh
az acr update --name contoso --data-endpoint-enabled
```

To view the data-endpoints, including regional endpoints for geo-replicated registries, use the az acr show-endpoints cli:

```sh
az acr show-endpoints --name contoso
```

outputs:

```json
{
    "loginServer": "contoso.azurecr.io",
    "dataEndpoints": [
        {
            "region": "eastus",
            "endpoint": "contoso.westus.data.azurecr.io",
        },
        {
            "region": "westus",
            "endpoint": "contoso.northeu.data.azurecr.io",
        }
    ]
}
```

### Azure Portal

Within the Azure Portal, select the Networking topic, selecting the Data endpoints tab to toggle **dedicated data-endpoints**.

<img src="./media/portal-dedicated-data-endpoints.png" width=700x>

Private Link is the most secure way to control network access between clients and the registry. When Private Link isn’t an option, dedicated data-endpoints can provide secure knowledge in what resources are accessible from each client.

## Pricing

Dedicated data-endpoints are a feature of premium registries.

For more [information on dedicated data-endpoints](https://aka.ms/acr/data-endpoints).
