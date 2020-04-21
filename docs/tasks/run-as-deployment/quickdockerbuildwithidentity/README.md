# Quick Docker build using identity and credential

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

## Grant identity access to custom registry 

```bash
#Get principal ID of the identity
principalId=$(az identity show --resource-group mytaskrunrg --name myquickdockerbuildrunwithidentity --query principalId --output tsv)

#Get the custom registry ID
customRegistryId=$(az acr show -n mycustomreg --query id --output tsv)

#Assign the Acrpull role to identity
az role assignment create \
  --assignee $principalId \
  --scope $customRegistryId \
  --role acrpull
```

## Deploy a quick run

```bash
#Get the custom registry name
customregistryName=$(az acr show -n mycustomreg --query loginServer --output tsv)

#Get resource ID of the identity
resourceId==$(az identity show --resource-group mytaskrunrg --name myquickdockerbuildrunwithidentity --query id --output tsv)

#Get client ID of the identity
clientId=$(az identity show --resource-group mytaskrunrg --name myquickdockerbuildrunwithidentity --query clientId --output tsv)

az deployment group create --resource-group "mytaskrunrg" --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json \
  --parameters registryName="myreg" \
  --parameters repository="hello-world" \
  --parameters taskRunName="mytaskrun" \
  --parameters userAssignedIdentity=$resourceId \
  --parameters customRegistry=$customregistryName \
  --parameters customRegistryIdentity=$clientId \
  --parameters sourceLocation="https://github.com/Azure-Samples/acr-build-helloworld-node.git"
```