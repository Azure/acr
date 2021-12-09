# Using Custom Domains with Azure Container Registry

**Important - Using a custom domain in Azure Container Registry is a private preview feature.**

**The Azure Container Registry team is not currently accepting new customers for this private preview. The feature will be made more widely available in the future.**

**If your registry has already been enabled for a custom domain and you need support, please open an issue in this repository.**  

Every ACR is accessed using its login server. If you have a registry called `myregistry`, you access it using its default hostname, `myregistry.azurecr.io` (in Azure Public Cloud.) As a customer belonging to an organization, you may prefer to access your registry using a custom domain that is associated with your organization, for instance, `container-registry.contoso.com`.

The following steps describe how you can achieve this.

**The following sections describe preparation steps for the private preview. THESE STEPS ARE NOT SUFFICIENT TO ENABLE A CUSTOM DOMAIN FOR YOUR REGISTRY WITHOUT ACCEPTANCE INTO THE PRIVATE PREVIEW.**

## Prerequisites
- [Azure CLI](https://docs.microsoft.com/cli/azure/?view=azure-cli-latest): version 2.4.0 or higher
  - Consider using [Azure Cloud Shell](https://docs.microsoft.com/azure/cloud-shell/overview)
- A _premium_ Azure Container Registry. See [here](https://docs.microsoft.com/azure/container-registry/container-registry-get-started-azure-cli) for instructions on how to create one.
- Your custom domain names. The following two are required:
  - Custom registry domain to access the registry REST API. Example for the `contoso.com` domain: `container-registry.contoso.com` 
  - Custom data domain to access the registry content. Again, example for `contoso.com`: `eastus-registry-data.contoso.com`
    - Note that the custom data domain is region specific. For geo-replicated registries, each region should have its own custom data endpoint.

  For each domain, you must prepare a single PEM formatted file containing the TLS private key and the public certificate:
  
  ```
  -----BEGIN PRIVATE KEY-----  
  XXXXXX  
  -----END PRIVATE KEY-----  
  -----BEGIN CERTIFICATE-----  
  XXXXXX  
  -----END CERTIFICATE-----
  ```
  
If you use a certificate bundle, prepare a single PEM formatted file containing the TLS private key and each public certificate:

```
---BEGIN PRIVATE KEY-----
XXXXXX
-----END PRIVATE KEY-------
-----BEGIN CERTIFICATE-----
XXXXX-01
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
XXXXXX-02
-----END CERTIFICATE----
[etc.]
```

  For example, using [openssl](https://github.com/openssl/openssl):
  - Create a self-signed public cert and private key
    ```shell
    openssl req -nodes -x509 -newkey rsa:4096 \
      -keyout container-registry.contoso.com.key.pem \
      -out container-registry.contoso.com.cert.pem -days 365 \
      -subj '/CN=container-registry.contoso.com/O=Contoso./C=US'
    ```
  - Create a single file containing both the public certificate (or certificates, in the case of a certificate bundle) and private key
    ```shell
    cat container-registry.contoso.com.key.pem \
      >> container-registry-contoso-com.pem
    cat container-registry.contoso.com.cert.pem \
      >> container-registry-contoso-com.pem
    ```
  - For each data domain, follow the same steps above to prepare the PEM formatted files containing the public certificate and private key.
  
  Azure Key Vault allows you to [create](https://docs.microsoft.com/azure/key-vault/certificate-scenarios) Certificate Authority (CA) signed certificates. 
  - If you choose to use the Azure Portal to create the certificates, be sure to select certificate content type as PEM.
 
## Prepare your existing registry
We will enable two features on your registry:
- Data Endpoints:\
  This feature provides a dedicated endpoint for downloading content from your registry. If you have a registry in East US, on enabling this feature, a data endpoint is automatically created for you: `myregistry.eastus.data.azurecr.io`
  
- ACR Managed Identities:\
  Managed Identities provide a mechanism to associate an Azure Active Directory identity with your registry, while relieving you of the burden of managing credentials. To learn more, see the documentation [here](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview).\
 ACR supports both user assigned and system assigned managed identities.

### Enable data endpoints and managed idenitites
1. `az login`
2. `az account set -s <subscription-id-or-name> `
3. `az acr update --data-endpoint-enabled true -n myregistry`
4. You can either enable a system assigned managed identity, a user assigned managed identity, or both for your registry. We recommend using system assigned managed identity to enable advanced scenarios with virtual networks that, although not supported currently, are [coming soon](#enhanced-security-with-virtual-networks). Do _one_ of the following:
   - To enable only system assigned managed identity: 
     - `az acr identity assign -n myregistry --identities [system]`
   - To enable user assigned managed identity, with or without a system identity: 
     - Create a user assigned managed identity following the instructions [here](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal).
     - Do _one_ of the following:
       - To enable _only_ user assigned managed identity:
         - `az acr identity assign -n myregistry --identities "<arm-resource-id-of-user-assigned-identity>"`
       - To enable _both_ user and system assigned managed identities:
         - `az acr identity assign -n myregistry --identities "<arm-resource-id-of-user-assigned-identity>" [system]`

## Prepare your Azure Key Vault
For each domain, its TLS private key and public certificate pair must be added to an Azure Key Vault that is accessible by your registry as a single PEM formatted file. We recommend creating a new key vault containing only your TLS certificates and granting the registry's identity access to `get` secret.
1. [Create](https://docs.microsoft.com/azure/key-vault/) a new Azure Key Vault.
2. [Add](https://docs.microsoft.com/azure/key-vault/certificate-scenarios) your certificates to the key vault.
3. Add an access policy to the key vault that grants your registry's identity access to `get` secret:\
   `az keyvault set-policy --name <your-kv-name> --secret-permissions get --spn <registry-system-or-user-mi-principal-id>`
   - The output of the command to enable managed identities on the registry will contain the principal ids of the assiged identities.
   - Alternatively, you may obtain the principal ids using `az cli`:
     - For system assigned managed identity:
       - `az acr show -n myregistry --query identity.principalId -o tsv`
     - For user assigned managed identities, you may list them as follows and use the desired principal ID:
       - `az acr show -n myregistry --query identity.userAssignedIdentities`

For greater isolation, we recommend that you put each certificate in its own key vault and set its access policy independently. The registry should always have access to the key vault secrets.

### Certificate updates and rotation

You have two options for updating the certificates used for custom domains:

* **Automatic updates** - If you reference a custom domain certificate with a [non-versioned](https://docs.microsoft.com/azure/key-vault/general/about-keys-secrets-certificates#objects-identifiers-and-versioning) secret ID, the registry regularly checks the key vault and automatically uses the latest certificate version there for its operations.

  To rotate or update a custom domain certificate, upload the new certificate version to the secret's location in the key vault. The registry automatically uses the latest certificate version within a short time. 
  
  > NOTE: after the certificate is updated, the registry may serve a mix of the old and new certificate versions for upto 15 minutes until all caches have been refreshed.

* **Manual updates** - If you reference a domain certificate with a [versioned](https://docs.microsoft.com/azure/key-vault/general/about-keys-secrets-certificates#objects-identifiers-and-versioning) secret ID, the registry does not configure automatic certificate rotation.

  After you upload a new certificate version to the key vault, the certificate must be manually rotated in the registry. Contact [Azure Support](https://azure.microsoft.com/support/create-ticket/).

### Enhanced security with Virtual Networks
Azure Key Vault allows you to [restrict access](https://docs.microsoft.com/azure/key-vault/key-vault-overview-vnet-service-endpoints) to specific virtual networks only. ACR custom domains are currently _not supported_ where key vault access is restricted, but this is work in progress and will be available with system managed identities only.
   
## Prepare your DNS zone
1. The custom registry domain must have a CNAME record with the target registry login server:\
   `container-registry.contoso.com` --> `myregistry.azurecr.io`
2. The regional custom data domain must have a CNAME record with the target regional registry data endpoint:\
   `eastus-registry-data.contoso.com` --> `myregistry.eastus.data.azurecr.io`
   - The output of the command to enable data endpoints on the registry will contain the regional data endpoint.
   
