## Getting Started with Azure Container Registry Transfer - in DotNetCore ##

This sample will allow you to transfer artifacts between two registries through a storage account. You will
* Create an exportPipeline in your source registry.
* Create an importPipeline in your target registry.
* Create key vault access policies for both pipeline identities.
* (Optionally) Run the exportPipeline to upload the artifacts to storage. 

Please see the [public documentation](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images) for more details on ACR Transfer.


#Prerequisities

* **Container registries**: This sample uses an existing source ACR with artifacts to transfer and a target registry. ACR Transfer is available in the **Premium** container registry service tier only.
* **Storage accounts**: Create source and target storage accounts that ACR Transfer will use to upload and download registry artifacts. Create a blob container for artifact transfer in each account.
* **Key Vaults**: Key Vaults are used to store SAS token secrets for export and import. Create the source and target key vaults in the same resource group as your source and target registries. Follow the instructions [here](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images#create-and-store-sas-keys) for instructions on how to create SAS tokens for export and import. Upload the export SAS token as a secret in your source key vault and the import SAS token as a secret in the target key vault.
* User Assigned Identities (optional): You may choose to create user assigned managed identity resources to assign to each pipeline [see tutorial](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-cli). Otherwise, ACR will automatically create a system assigned identity for each of the exportPipeline and importPipeline resources. Read more about the difference between user assigned and system assigned identities [here](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview). Note: You do not need to perform RBAC on these managed identities resources. The sample will add an access policy for each pipeline managed idenity so that it can fetch secrets from the necessary key vault.

Note: The sample assumes the above resources are all in the same subscription.

## Running this sample ##

* Create a service principal and assign it the contributor role of the source and target resource groups.
```
az ad sp create-for-rbac -n "MyApp" --sdk-auth --role contributor \
    --scopes /subscriptions/{SubID}/resourceGroups/{SourceResourceGroup} \
    /subscriptions/{SubID}/resourceGroups/{TargetResourceGroup}
```

Make note of `clientId`, `clientSecret`, `subscriptionId`, and `tenantId` to add to appsettings.json.

```
  "TenantId": "",
  "SPClientId": "",
  "SPClientSecret": "",
  "SubscriptionId": "",
```

Update the ExportPipeline and ImportPipeline sections of appsettings.json with your source and target configs respectively:

```
    "ResourceGroupName": "",
    "RegistryName": "",
    "PipelineName": "",
    "KeyVaultUri": "",
    "ContainerUri": "",
    "UserAssignedIdentity": "",
    "Options": []
```

Where `KeyVaultUri` is the key vault SAS secret uri and `ContainerUri` is the storage container uri. `PipelineName` is the name you choose for the exportPipeline or importPipeline resources. For more info on `Options` please refer to [Export options](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images#export-options) and [Import options](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images#import-options).


* If you intend to run the exportPipeline created in this sample, please fill out the following section of appsettings.json:

```
"ExportPipelineRun": {
    "PipelineRunName": "",
    "TargetName": "",
    "Artifacts": []
  }
```

Where `TargetName` is the name you choose for the artifacts blob exported to your source storage account, such as 'myblob'. `PipelineRunName` is the name you choose for the pipelineRun resource.
And `Artifacts` is the list of up to 50 artifacts that you would like to transfer from your source registry.
Example: `[samples/hello-world:v1", "samples/nginx:v1" , "myrepository@sha256:0a2e01852872..."]`



* Build ContainerRegistryTransfer.csproj (DotNetCore SDK 3.1 required)
```
dotnet build ContainerRegistryTransfer/ContainerRegistryTransfer.csproj

dotnet ContainerRegistryTransfer/bin/Debug/netcoreapp3.1/ContainerRegistryTransfer.dll
```

## More information ##

[https://github.com/Azure/azure-sdk-for-net](https://github.com/Azure/azure-sdk-for-net)

If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212).
