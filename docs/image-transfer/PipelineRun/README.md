# Run the pipelines

Export pipelines and import pipelines must be invoked in order to perform the export or import job. These pipelines are both invoked by creating a PipelineRun resource. While ExportPipelines must be run manually with PipelineRun, ImportPipelines are configured by default to run automatically once artifacts land in the target storage account.

## Run the pipeline

The resource ID of the pipeline to be run must be specified in the parameters.

When running an export pipeline, the following parameters are required
    * pipelineResourceId
    * targetName (the name of the target blob)
    * artifacts

When running an import pipeline, the following parameters are required
    * pipelineResourceId
    * sourceName (the name of the source blob)

The below example runs an export pipeline. 

```
az deployment group create \
  --resource-group myResourceGroup \
  --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json
```
