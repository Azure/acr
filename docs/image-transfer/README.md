# ACR Transfer

ACR Transfer enables artifact movement between source and target registries via storage account. This feature can be leveraged to transfer multiple artifacts at once and to move those artifacts across tenants.

The following 3 resources are used for ACR Transfer. All are created using PUT operations.
* **ExportPipeline** : long lasting resource that contains high level target info, such as storage blob container URI and the KV secret URI of the target storage SAS token.
* **ImportPipeline** : long lasting resource that contains high level source info, such as storage blob container URI and the KV secret URI of the source storage SAS token. Source trigger is enabled by default so the pipeline will run automatically when artifacts land in the source storage container.
* **PipelineRun** : resource used to invoke either an ExportPipeline or ImportPipeline resource. 
    * ExportPipelines must be run manually by creating an PipelineRun resource.  Only when you run the ExportPipeline do you specify the artifacts to be exported. 
    * ImportPipelines configured with source trigger enabled are run automatically. They can also be run manually using PipelineRun.

## Prerequisites

### Resources
* Create a storage account used to transfer the artifacts. If the source and target registries are in different tenants, please create a storage account in each tenant. 
* Create a key vault used to store the SAS token secret for your storage account. If the source and target registries are in different tenants, please create a key vault in each tenant. 

### Create SAS Token

ACR Transfer users SAS tokens to export to and import from storage accounts. We retrieve this token from Key Vault using the pipeline's managed identity. Prior to running each pipeline, ensure these SAS tokens are uploaded to Key Vault and are valid. The properties required for each scenario are detailed below:


**Export**

SAS Properties:

 *   **Allowed Services** :	Blob

*    **Allowed Resource Types** :	Container/Object

*    **Allowed Permissions** :	Read/Write/List/Add/Create

Put export SAS token in KV as a secret.

```
az keyvault secret set \
	 --name acrexportsas \ 
	--value $EXPORT_SAS \ 
--vault-name mykeyvault 
```

**Import**

SAS Properties:

 *   **Allowed Services** :	Blob

*    **Allowed Resource Types** :	Service/Container/Object

*    **Allowed Permissions** :	Read/List/Delete

Put import SAS token in KV as a secret.

```
az keyvault secret set \
	 --name acrimportsas \ 
	--value $IMPORT_SAS \ 
--vault-name mykeyvault 
```