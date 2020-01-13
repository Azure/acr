# Customer-Managed Key (CMK)

Customer-Managed Key (CMK) or Bring your own key (BYOK) allows customers to configure customer-managed keys for Azure Container Registry (ACR) encryption. All customer-managed keys must be stored in an Azure Key Vault.

In this version of the API, you can create a new Premium container registry with CMK can be enabled using an Azure ARM template. 

> **Note:** The  CLI  and portal experience will light up soon.

### Current limitations

* Disabling encryption for a registry is not supported.
* Other registry features like geo-replication, content-trust and VNet integration will also be supported in a future release
* This feature is only enabled on a newly created registry

## Register the provider to use the feature

In order to try out this new feature, you will need to sign-up using the command below. Once you run the command, product team will get notified of your request and we will approve the request.

You can use the feature after the request gets approved.

```bash
az feature register -n PrivatePreview --namespace Microsoft.ContainerRegistry --subscription <subscriptionId>
```

## Deploy a registry with CMK enabled

Follow these steps to create a registry with CMK enabled using an ARM template. Note that we will create a Key Vault and Key using CLI as we do not have ARM support for these scenarios.

### 1. Create a resource group

Create a resource group for creating the Key Vault and Keys.

```bash
az group create -g <resource-group-name> -l <location>
```

### 2. Create a key vault

Create a key vault to store customer-managed keys for registry encryption. This key vault should have two key protection settings enabled - Soft Delete and Do Not Purge. 

```bash
az keyvault create –-name <key-vault-name> -g <resource-group-name> --enable-soft-delete --enable-purge-protection
```

### 3. Create a key and get the key ID

Create a key and get the key ID.

```bash
 KEK=$(az keyvault key create --name <key-name> --vault-name <key-vault-name> --query key.kid -o tsv)
 ```

### 4. Create a registry with CMK enabled

Download the [template.json file](https://github.com/Azure/acr/blob/master/docs/CMK/template.json). Run the following command to create a registry with CMK enabled. Note that you need to provide the key vault name that you just created. Registry and user assigned managed identity will be created by the template.

```bash
az group deployment create -g <resource-group-name> --template-file <template.json> --parameters vault_name=<key-vault-name> registry_name=<registry-name> identity_name=<managed-identity> kek_id=$KEK
```

### 5. Key rotation

You can rotate keys by creating a new key using step 3 and then using the new key in step 4 mentioned above.

### 6. Check out registry encryption settings

```bash
az resource show --id <registry-resource-id> --query properties.encryption --api-version 2019-12-01-preview
```

## Login and use the registry

### 1. Login to your registry

```json
az acr login -n myacr
```

Follow [this documentation](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-authentication) for authenticate your docker client to your container registry.

### 2. Push images to your registry

For pushing docker images on your registry, follow [this documentation](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-docker-cli)

## Feedback

For feedback on CMK, visit https://aka.ms/acr/issues.
