# Quick Docker build using identity and keyvault

## Create a resource group

```bash
az group create \
  -n mytaskrunrg \
  -l westus
```

## Create a Registry

```bash
az acr create \
   -n myreg -g mytaskrunrg --sku Standard
```

## Create a Custom Registry

```bash
az acr create \
   -n mycustomreg -g mytaskrunrg --sku Standard
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
#Get principal id of the identity
principalId=$(az identity show --resource-group mytaskrunrg --name myquickdockerbuildrunwithidentity --query principalId --output tsv)

az keyvault set-policy --name mykeyvault --resource-group mytaskrunrg --object-id $principalId --secret-permissions get 
```

## Deploy a quick run

```bash
#Get the custom registry name
customregistryName=$(az acr show -n mycustomreg --query loginServer --output tsv)

#Get the KeyVault UserName Url
userNameUrl=$(az keyvault secret show --name username --vault-name mykeyvault --query id --output tsv)

#Get the KeyVault Password Url
passwordUrl=$(az keyvault secret show --name password --vault-name mykeyvault --query id --output tsv)

#Get the ID of ManagedIdentity
managedId=$(az identity show --resource-group mytaskrunrg --name myquickdockerbuildrunwithidentity --query id --output tsv)

az group deployment create --resource-group "mytaskrunrg" --template-file azuredeploy.json \
	--parameters azuredeploy.parameters.json \
	--parameters registryName="myreg" \
	--parameters taskRunName="mytaskrun" \
	--parameters customRegistryName=$customregistryName \
	--parameters userNameUrl=$userNameUrl \
	--parameters userPasswordUrl=$passwordUrl \
	--parameters repository="hello-world" \
	--parameters managedIdResourceId=$managedIdResourceId \
	--parameters sourceLocation="https://github.com/Azure-Samples/acr-build-helloworld-node.git"
```

