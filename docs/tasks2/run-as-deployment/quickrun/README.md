# Quick run

Deploy a quick run or a set of container using a multi-step task with Managed Identities. 

## Create a resource group

```bash
az group create \
  -n mytaskrunrg \
  -l westus
```

## Create a user assigned identity
```sh
identity=$(az identity create \
  -g mytaskrunrg \
  -n mytaskrunidentity \
  --query 'id' \
  -o tsv)
```

## Deploy a registry and a task run  which is associated with the user assigned identity and run a multi-step task

```bash
registry=$(az group deployment create \
  -g mytaskrunrg \
  --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json \
  --parameters userAssignedIdentity=$identity \
  --query 'properties.outputs.registry.value' \
  -o tsv)
```

## Output the run log

```bash
az acr task list-runs -r $registry --query '[0].runId' -o tsv |\
  xargs -I% az acr task logs -r $registry --run-id %
```
