# Bring Your Own Key (BYOK)
Bring your own key feature will allow customers to configure customer-managed keys for Azure Container Registry (ACR) encryption. 

In this private preview, you can create a new container registry with BYOK enabled using Azure CLI and ARM template. See details below on how to create an Azure Container Registry with BYOK enabled.
 
> Note: This is an early preview release. The full CLI expereince and portal experience will light up soon. Also, note that

>Note: Some features like disabling encryption, geo- replication, content-trust are not supported in this version.

 
 
# Steps to create a registry with BYOK enabled

## 1. Create a managed identity 

Create a managed identity using the CLI command below.


```json
az identity create --name <managed-identity-name>
```

## 2. Create a Key Vault 
The key vault that you use to store customer-managed keys for registry encryption must have two key protection settings enabled, Soft Delete and Do Not Purge. 

```json
az keyvault create \
 –-name <key-vault> \
 --enable-soft-delete \
 --enable-purge-protection
```

## 3. Create a Key

```json
az keyvault key create –-name <key> --vault-name <key-vault>
```
## 4. Assign policy to KV

Next, configure the access policy for the key vault so that the registry has permissions to access it. In this step, you'll use the managed identity that you previously assigned to the registry. Key permissions need to be set to get, recover, wrapKey and unwrapKey. 

```json
az keyvault set-policy --name <key-valut> -g <resource-gropup-name> --object-id <principalId> --key-permission get recover unwrapKey wrapKey

```

## 5. Create an ACR with BYOK enabled

```json
    az resource create -g reshmim -n reshmimacr --resource-type Microsoft.ContainerRegistry/registries --is-full-object --api-version 2019-12-01-preview --properties "{\"location\": \"centralus\", \"sku\": {\"name\": \"Premium\"},\"properties\": {\"encryption\": {\"status\": \"enabled\", \"keyVaultProperties\": {\"keyIdentifier\": \"https://reshmim-kv.vault-int.azure-int.net/keys/reshmim-kek/afefad2560b64a18a813295b58259b31\",\"identity\": \"1ca05082-78f6-4834-8710-4cd5c8cbad70\"}}}, \"identity\":{\"type\": \"SystemAssigned, UserAssigned\",\"userAssignedIdentities\":{\"/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourcegroups/reshmim/providers/Microsoft.ManagedIdentity/userAssignedIdentities/reshmim-id\":{}}}}"
    
```
 
# Steps to create a registry with BYOK enabled using ARM template

# 1 Create a KV using CLI step above

# 2 Deploy ARM template



## End
