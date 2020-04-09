# Using Custom Domains with Azure Container Registry

Every ACR is accessed using its login server. If you have a registry called `myregistry`, you access it using its default hostname, `myregistry.azurecr.io` (in Azure Public Cloud.) As a customer belonging to an organization, you may prefer to access your registry using a custom domain that is associated with your organization, for instance, `container-registry.contoso.com`.

The following steps describe how you can achieve this.

## Prerequisites
- [Azure CLI](https://docs.microsoft.com/cli/azure/?view=azure-cli-latest), [curl](https://curl.haxx.se/) (or a similar tool)
  - Consider using [Azure Cloud Shell](https://docs.microsoft.com/azure/cloud-shell/overview)
- A _premium_ Azure Container Registry. See [here](https://docs.microsoft.com/azure/container-registry/container-registry-get-started-azure-cli) for instructions on how to create one.
- Your custom domain names. The following two are required:
  - custom registry domain to access the registry REST API: `container-registry.contoso.com` 
  - custom data domain to access the registry content: `eastus-registry-data.contoso.com`
    - Note that the custom data domain is region specific. For geo-replicated registries, each region should have its own custom data endpoint.

  For each domain, you must prepare a single PEM formatted file containing the TLS private key and public certificate:
  
  ```
  -----BEGIN PRIVATE KEY-----  
  .....  
  -----END PRIVATE KEY-----  
  -----BEGIN CERTIFICATE-----  
  .....  
  -----END CERTIFICATE-----
  ```
  
  For example, using [openssl](https://github.com/openssl/openssl):
  - Create a self-signed public cert and private key
     - `openssl req -nodes -x509 -newkey rsa:4096 -keyout container-registry.contoso.com.key.pem -out container-registry.contoso.com.cert.pem -days 365 -subj '/CN=container-registry.contoso.com/O=Contoso./C=US'`
  - Create a single file containing both the public certificate and private key
     - `cat container-registry.contoso.com.key.pem >> container-registry-contoso-com-pem`
     - `cat container-registry.contoso.com.cert.pem >> container-registry-contoso-com-pem`
  
  Azure Key Vault allows you to [create certificates](https://docs.microsoft.com/azure/key-vault/certificate-scenarios) signed with a Certificate Authority.
  
## Prepare your existing registry
We will enable two features on your registry that are currently in preview:
- Data Endpoints:\
  This feature provides a dedicated endpoint for downloading content from your registry. If you have a registry in East US, on enabling this feature, a data endpoint is automatically created for you: `eastus.data.myregistry.azurecr.io`
  
- ACR Managed Identities:\
  Managed Identities (MI) provide a mechanism to associate an Azure Active Directory identity with your registry, while relieving you of the burden of managing credentials. To learn more, see the documentation [here](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview).\
 ACR supports both user assigned and system assigned MI. If you choose to enable user assigned MI for your registry, create one first before proceeding. Instructions [here](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal).

### Enable preview features
1. `az login`
2. `az account set -s <subscription-id-or-name> `
3. `apiVersion="api-version=2019-12-01-preview"`
4. `mgmtEndpoint=$(az cloud show --query endpoints.resourceManager -o tsv)`
5. `accessToken=$(az account get-access-token --query accessToken -o tsv)`
6. `registryResourceId=$(az acr show -n myregistry --query id -o tsv)`
7. You can either enable a system assigned MI, or a user assigned MI, or both for your registry. We recommend using a system assigned MI to enable advanced scenarios with virtual networks that are [called out below](#enhanced-security-with-virtual-networks). Do one of the following:
   - System assigned: 
     - `identity="{\"type\": \"systemAssigned\"}"`
   - User assigned: 
     - `userMIResourceId=$(az identity show -n <user-mi-name> -g <user-mi-resource-group> --subscription <user-mi-subscription-id-or-name> --query id -o tsv)`
     - `identity="{\"type\": \"userAssigned\", \"userAssignedIdentities\": {\"${userMIResourceId}\":{}}}"`
   - Both:
     - `userMIResourceId=$(az identity show -n <user-mi-name> -g <user-mi-resource-group> --subscription <user-mi-subscription-id-or-name> --query id -o tsv)`
     - `identity="{\"type\": \"systemAssigned,userAssigned\", \"userAssignedIdentities\": {\"${userMIResourceId}\":{}}}"`
8. `payload="{\"identity\": $identity, \"properties\": {\"dataEndpointEnabled\": true}}"`
9. `endpoint="$mgmtEndpoint$registryResourceId?$apiVersion"`
10. `curl -f -X PATCH -H "Authorization: Bearer $accessToken" -H "Content-Type: application/json" $endpoint -d "$payload"`

## Prepare your Azure Key Vault
For each domain, its TLS certificate must be added to an Azure Key Vault that is accessible by your registry. We recommend creating a new key vault containing only your TLS certificates and granting the registry's identity access to `get` secret.
1. [Create](https://docs.microsoft.com/azure/key-vault/) a new Azure Key Vault.
2. [Add](https://docs.microsoft.com/azure/key-vault/certificate-scenarios) your certificates to the key vault.
3. Add an access policy to the key vault that grants your registry's identity access to `get` secret:\
   `az keyvault set-policy --name <your-kv-name> --secret-permissions get --spn <registry-system-or-user-mi-client-id>`
   - The output of the command to enable preview features on the registry will contain these client ids.

For greater isolation, we recommend that you put each certificate in its own key vault and set its access policy independently. The registry should always have access to the TLS certificates.

### Enhanced security with Virtual Networks
Azure Key Vault allows you to [restrict access](https://docs.microsoft.com/azure/key-vault/key-vault-overview-vnet-service-endpoints) to specific virtual networks only. ACR custom domains are currently _not supported_ where key vault access is restricted, but this is work in progress and will be available with system managed identities only.
   
## Prepare your DNS zone
1. The custom registry domain must have a CNAME record with the target registry login server:\
   `container-registry.contoso.com` --> `myregistry.azurecr.io`
2. The regional custom data domain must have a CNAME record with the target regional registry data endpoint:\
   `eastus-registry-data.contoso.com` --> `eastus.data.myregistry.azurecr.io`
   - The output of the command to enable preview features on the registry will contain the regional data endpoint.
   
## Contact us
As a final step, share the following with us by creating a support ticket ([Azure Support](https://azure.microsoft.com/en-us/support/create-ticket/)):
- Custom registry domain details
  - custom registry domain (`container-registry.contoso.com`)
  - key vault secret ID of the corresponding TLS certificate
  - Client ID of the user assigned registry identity that has access to this secret (not required in case of system assigned)
- Custom data domain details
  - regional custom data domain (`eastus-registry-data.contoso.com`)
  - key vault secret ID of the corresponding TLS certificate
  - Client ID of the user assigned registry identity that has access to this secret (not required in case of system assigned)
