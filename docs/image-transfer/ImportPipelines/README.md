# Export Pipeline

Create an ImportPipeline resource in your target container registry. Note that the import will not take place until the import pipeline is run. ImportPipelines configured with source trigger enabled are run automatically once a new artifact lands in the target storage account. They can also be run manually using PipelineRun resource.

## Create the managed identity

Note: These examples are implemented with user-assigned identities. ExportPipelines and ImportPipelines also support SystemAssigned identities. If using system identity, please handle role assignment after the pipeline resources are created and prior to running.

### Create the user-assigned managed identity

```
az identity create --resource-group myResourceGroup --name myPipelineId 
```
```
principalID=$(az identity show --resource-group myResourceGroup --name myPipelineId --query principalId --output tsv)
resourceID=$(az identity show --resource-group myResourceGroup --name myPipelineId --query id --output tsv)
```

### Grant the identity read access to KV

```
az keyvault set-policy --name mykeyvault \ --resource-group myResourceGroup \ --object-id $principalID \ --secret-permissions get
```

## Create the ImportPipeline

```
az group deployment create \
  --resource-group myResourceGroup \
  --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json
  --parameters userAssignedIdentity=$resourceID
```