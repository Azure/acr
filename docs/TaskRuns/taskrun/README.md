# Task run

## Create a resource group

```bash
az group create \
  -n mytaskrunrg \
  -l westus
```

## Deploy a task run, which will create the registry, task and schedule a run using the following command

```bash
az group deployment create \
  --resource-group "mytaskrunrg" --template-file azuredeploy.json --parameters azuredeploy.parameters.json \
  --parameters registryName="mytaskrunrg" --parameters --parameters taskName="huanwudfwesttask02" taskRunName="mytaskname"
```

