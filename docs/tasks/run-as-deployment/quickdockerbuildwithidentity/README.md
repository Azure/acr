# Quick Docker build using identity and credential

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

## Add role assignment to the remote registry (You need fill the right information below)

```bash
az role assignment create --assignee "c5e2807e-4bbf-4faa-80fb-0a37f316113a" \
  --scope "/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourceGroups/huanwutest/providers/Microsoft.ContainerRegistry/registries/huanwudftest2" \
  --role acrpull
```

## Deploy a quick run
Fill the right credential azuredeploy.json

```bash
az group deployment create --resource-group "huanwutest" --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json --parameters registryName="huanwudftest6" --parameters taskRunName="huanwudfwesttaskrun03" \
  --parameters userAssignedIdentity="/subscriptions/f9d7ebed-adbd-4cb4-b973-aaf82c136138/resourcegroups/huanwudfwestgroup/providers/Microsoft.ManagedIdentity/userAssignedIdentities/huanwudfidentity"
```

