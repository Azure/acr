# Use a custom domain with Azure Container Registry (private preview)

This article describes the steps to set up the endpoints of an Azure container registry using your own custom domain name, such as **contoso.com**.

By default, every Azure container registry is accessed externally using its login server name. If you have a registry called `myregistry`, you access it using its default hostname, `myregistry.azurecr.io` (in the Azure Cloud). As a customer belonging to an organization, you may prefer to access your registry using a custom domain name that is associated with your organization, for example, `myregistry.contoso.com`.

**Important - Using a custom domain name in Azure Container Registry is currently a private preview feature**. It can only be enabled by preparing certificates, a Premium container registry, and an Azure key vault as described in this article, and then opening an Azure support request to complete the configuration. 

## Limitations

* This capability is provided without a service level agreement, and isn't currently recommended for production workloads.
* Azure Container Registry custom domains can't currently be used where key vault access is restricted. 

## Prerequisites
- [Azure CLI](https://docs.microsoft.com/cli/azure/): version 2.4.0 or higher, or use [Azure Cloud Shell](https://docs.microsoft.com/azure/cloud-shell/overview)
- An Azure container registry in the Premium service tier. See [this article](https://docs.microsoft.com/azure/container-registry/container-registry-get-started-azure-cli) for instructions to create one. If you have an existing registry, you can [upgrade](https://docs.microsoft.com/azure/container-registry/container-registry-skus#changing-tiers).
- Your custom domain names. The following two are required:
  - Custom registry domain to access the registry REST API. Example for the `contoso.com` domain: `container-registry.contoso.com` 
  - Custom data domain to access the registry content. Example for `contoso.com`: `eastus-registry-data.contoso.com`

      **Note** - The custom data domain is region specific. If your registry is geo-replicated, each region should have its own custom data domain.

## Prepare certificates
  
For each domain,  prepare a single PEM-formatted file containing the TLS private key and public certificate. The private key must be exportable.

 ```
  -----BEGIN PRIVATE KEY-----  
  .....  
  -----END PRIVATE KEY-----  
  -----BEGIN CERTIFICATE-----  
  .....  
  -----END CERTIFICATE-----
  ```

You may use your own valid certificates signed by a certificate authority (CA) or self-signed certificates. Self-signed certificates should only be used for test and evaluation purposes.
  
To create self-signed certificates:
1. Use [openssl](https://github.com/openssl/openssl) to create a self-signed public certificate and private key.

   ```shell
    openssl req -nodes -x509 -newkey rsa:4096 \
      -keyout container-registry.contoso.com.key.pem \
      -out container-registry.contoso.com.cert.pem -days 365 \
      -subj '/CN=container-registry.contoso.com/O=Contoso./C=US'
   ```

2. Create a single file containing both the public certificate and private key.
   ```shell
    cat container-registry.contoso.com.key.pem \
       >> container-registry-contoso-com.pem
    cat container-registry.contoso.com.cert.pem \
       >> container-registry-contoso-com.pem
    ```
  3. For each data domain in your registry, repeat the preceding steps to prepare a PEM-formatted file containing the public certificate and private key.
  
You can also use Azure Key Vault to [create](https://docs.microsoft.com/azure/key-vault/certificate-scenarios) CA-signed certificates. If you choose to use the Azure Portal to create the certificates, be sure to select the PEM content type for each certificate.
 
## Prepare your existing registry

You need to enable two features on your registry:
- [Dedicated data endpoints](https://docs.microsoft.com/azure/container-registry/container-registry-firewall-access-rules#enable-dedicated-data-endpoints) -  If you have a registry in East US, after enabling this feature, a data endpoint is automatically created for you: `myregistry.eastus.data.azurecr.io`
  
- [Managed identities](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview) - Managed identities associate an Azure Active Directory identity with your registry, relieving you of the burden of managing credentials to access certain Azure resources.
 
  Azure Container Registry supports both user-assigned and system-assigned managed identities. We recommend using a system-assigned managed identity to access secrets for a custom domain managed in Azure Key Vault.

### Enable data endpoints and managed identities

Use the Azure CLI:
1. `az login`
2. `az account set -s <subscription-id-or-name> `
3. To enable dedicated data endpoints:

   `az acr update --data-endpoint-enabled true -n myregistry`
1.  To enable a managed identity, do _one_ of the following:
    - To enable only the system-assigned managed identity: 
    
      `az acr identity assign -n myregistry --identities [system]`
    - To enable a user-assigned managed identity, with or without a system-assigned identity: 
      - [Create](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal) a user-assigned managed identity.
      - Get the resource ID of the user-assigned identity. 
          - In the portal, go to the identity and select **Properties** under **Settings**.
          - Alternatively, you may obtain the resource ID using the `az identity show` command in the Azure CLI.
      - Do _one_ of the following:
        - To enable _only_ the user-assigned managed identity:
        
          `az acr identity assign -n myregistry --identities "<arm-resource-id-of-user-assigned-identity>"`
        - To enable _both_ user- and system-assigned managed identities:
         
          `az acr identity assign -n myregistry --identities "<arm-resource-id-of-user-assigned-identity>" [system]`

## Prepare your Azure key vault

For each domain, the corresponding TLS private key and public certificate pair must be added as a PEM-formatted file to an Azure key vault that is accessible by your registry. We recommend creating a new key vault containing only your TLS certificates and granting the registry's identity access to `get` secrets.
1. [Create](https://docs.microsoft.com/azure/key-vault/) a new Azure key vault.
2. [Import](https://docs.microsoft.com/en-us/azure/key-vault/certificates/tutorial-import-certificate) certificate PEM files you created to the key vault. Alternatively, generate certificates in the key vault.
3. Add an access policy to the key vault that grants your registry's identity access to `get` secrets. This policy enables the registry to access the private key portion of the certificate, which is addressed as a secret.\
   `az keyvault set-policy --name <your-kv-name> --secret-permissions get --spn <registry-system-or-user-mi-principal-id>`
   - The output of the command to enable managed identities on the registry contains the principal IDs of the assiged identity or identities.
   - Alternatively, you may obtain the principal IDs using the Azure CLI:
     - For system-assigned managed identity:
       - `az acr show -n myregistry --query identity.principalId -o tsv`
     - For user-assigned managed identities, you may list them as follows and use the desired principal ID:
       - `az acr show -n myregistry --query identity.userAssignedIdentities`

For greater isolation, we recommend that you put each certificate in its own key vault and set its access policy independently. The registry should always have access to the key vault certficates.

### Enhanced security with virtual networks
Azure Key Vault allows you to [restrict access](https://docs.microsoft.com/azure/key-vault/key-vault-overview-vnet-service-endpoints) to specific virtual networks only. Azure Container Registry custom domains can't currently be used where key vault access is restricted. This work is in progress and will be available with system-managed identities only.
   
## Prepare your DNS zone
1. The custom registry domain must have a CNAME record with the target registry login server:\
   `container-registry.contoso.com` --> `myregistry.azurecr.io`
2. Each regional custom data domain must have a CNAME record with the target regional registry data endpoint:\
   `eastus-registry-data.contoso.com` --> `myregistry.eastus.data.azurecr.io`
   - The output of the command to enable data endpoints on the registry will contain the regional data endpoint.
   
## Contact Azure support
As a final step, share the following information with us by creating an [Azure Support](https://azure.microsoft.com/support/create-ticket/) ticket. This information is needed to complete the custom domain configuration.
- **Custom registry domain details**
  - custom registry domain name (`container-registry.contoso.com`)
  - key vault secret ID of the corresponding TLS data (a URI of the form `https://myvaultvault.azure.net/secrets/myregdomain/xxxxxxxxxxxxx`)
  - client ID of the user-assigned registry identity that has access to this secret (not required in case of system-assigned identity)
- **Custom data domain details, for each data domain**
  - regional custom data domain name (`eastus-registry-data.contoso.com`)
  - key vault secret ID of the corresponding TLS data (a URI of the form `https://myvaultvault.azure.net/secrets/myregdomain/xxxxxxxxxxxxx`)
  - client ID of the user-assigned registry identity that has access to this secret (not required in case of system-assigned identiity)
