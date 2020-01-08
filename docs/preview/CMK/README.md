# Customer-Managed Key (CMK)

Customer-Managed Key (CMK) or Bring your own key (BYOK) allows customers to configure customer-managed keys for Azure Container Registry (ACR) encryption. All customer-managed keys must be stored in an Azure Key Vault.

In this private preview, you can create a new Premium container registry with BYOK enabled using an Azure ARM template. 
 
> **Note:** This is an early private preview release. The full CLI expereince and portal experience will light up soon. See [details](#how-to-sign-up-for-a-private-preview) on how you can sign-up for a private preview of BYOK.

## Limitations for private preview

* Disabling encryption for a registry is not supported in this private preview 
* Other registry features like geo-replication, content-trust and VNet integration will also be supported in a future release
* Do not enable this feature on production environment
* This feature is only enabled on a newly created registry

## How to sign-up for a private preview?

In order to try out this new feature in private preview, you will need to sign-up using the command below. Once you run the command, product team will get notified of your request and we will approve the request.

You can use the feature after the request gets approved.

```json
az feature register -n PrivatePreview --namespace Microsoft.ContainerRegistry --subscription <subscriptionId>
```

## Deploy a registry with BYOK enabled

Follow these steps to create a registry with BYOK enabled using an ARM template. Note that we will create a Key Vault and Key using CLI as we do not have ARM support for these scenarios.

### 1. Create a resource group

Create a resource group for creating the Key Vault and Keys.

```json
az group create -g <resource-group-name> -l <location> 
```

### 2. Create a key vault

Create a key vault to store customer-managed keys for registry encryption. This key vault should have two key protection settings enabled - Soft Delete and Do Not Purge. 

```json
az keyvault create \
 –-name <key-vault-name> \
 -g <resource-group-name> \
 --enable-soft-delete \
 --enable-purge-protection
```

### 3. Create a key and get the key ID
	 
 Create a key and get the key ID.
 
 ```json
 KEK=$(az keyvault key create --name <key-name> --vault-name <key-vault-name> --query key.kid -o tsv)
 ```
 
 ### 4. Create a registry with CMK enabled

Download the template.json file. Run the following command to create a registry with BYOK enabled. Note that you need to provide the key vault name that you just created. Registry and user assigned managed identity will be created by the template.
  
```json
az group deployment create -g <resource-group-name> --template-file <template.json> --parameters vault_name=<key-vault-name> registry_name=<registry-name> identity_name=<managed-identity> kek_id=$KEK
```

### 5. Key rotation

You can rotate keys by creating a new key using step 3 and then using the new key in step 4 mentioned above.


## Login and use the registry

### 1. Login to your registry

```json
az acr login -n myacr
```
Follow [this documentation](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-authentication) for authenticate your docker client to your container registry.

### 2. Push images to your registry

For pushing docker images on your registry, follow [this documentation](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-docker-cli)

## Feedback

For feedback on BYOK, visit https://aka.ms/acr/issues.

