# Quick docker build on an existing registry

The sample shows how to schedule a deployment which will perform a quick docker build on an existing registry from a source located in a GitHub repository. The tag of the image is derived from the `taskRunName` provided during deployment.

## Deploy the Task

```bash
registry=$(az group deployment create \
  -g mytaskrunrg \
  --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json \
  --parameters taskRunName=mytaskrunwithidentity \
  --query 'properties.outputs.registry.value' \
  -o tsv)
```
## 

```bash
export registry=mytaskrunregistry
export repository=helloworld-node
az acr repository show-tags -n $registry --repository $repository --detail -o table
```