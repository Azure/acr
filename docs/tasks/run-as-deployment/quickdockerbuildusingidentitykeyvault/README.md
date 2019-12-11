# Quick Docker build using identity and keyvault

## Create a resource group

```bash
az group create \
  -n mytaskrunrg \
  -l westus
```

## Crate a Registry

```bash
az acr create \
   -n myreg -g mytaskrunrg --sku Standard
```

## Create a User Identity

```bash
az identity create \
  -g mytaskrunrg \
  -n myquickdockerbuildrunwithidentity
```

## Create KeyVault

```bash
az keyvault create --name mykeyvault --resource-group mytaskrunrg --location eastus2
```

## Save registry username/password in the keyvault

```bash
az keyvault secret set --name username --value <username> --vault-name mykeyvault
az keyvault secret set --name password --value <password> --vault-name mykeyvault
```

## Grant identity access to key vault (object-id is the Object ID of managed identity)
```bash
az keyvault set-policy --name mykeyvault --resource-group mytaskrunrg --object-id 452e1d96-c423-4da0-99f2-d3a1789ab69f --secret-permissions get 
```

## Deploy a quick run (Fill the right information in azuredeploy.json)

```bash
az group deployment create --resource-group "mytaskrunrg" --template-file azuredeploy.json \
	--parameters azuredeploy.parameters.json \
	--parameters registryName="myreg" \
	--parameters taskRunName="mytaskrun" \
	--parameters managedIdentityName="myquickdockerbuildrunwithidentity" \
	--parameters customRegistryName="huanglitest05.azurecr-test.io" \
	--parameters userNameUrl="https://huanglikeyvault3.vault-int.azure-int.net/secrets/UserName" 
	--parameters userPasswordUrl="https://huanglikeyvault3.vault-int.azure-int.net/secrets/Password"
```

