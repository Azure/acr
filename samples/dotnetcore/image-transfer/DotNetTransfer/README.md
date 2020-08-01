## Getting Started with Azure Container Registry Transfer - in DotNetCore ##

This sample will allow you to transfer artifacts between two registries. You will
* Create an exportPipeline in your source registry.
* Create an importPipeline in your target registry.
* Create key vault access policies for both pipeline identities.
* (Optionally) Run the exportPipeline.

Please see the [public documentation](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images) for more details on ACR Transfer.

## Running this sample ##


* Create a service principal and assign it the contributor role of the subscription
```
az ad sp create-for-rbac -n "MyApp" --role contributor \
    --scopes /subscriptions/{SubID}/resourceGroups/{ResourceGroup1} \
    /subscriptions/{SubID}
```

Make note of `clientId`, `clientSecret`, and `tenantId` to add to appsettings.json

```
  "TenantId": "",
  "SPClientId": "",
  "SPClientSecret": "",
  "SubscriptionId": "",
```

* Container registries: This sample uses an existing source ACR with artifacts to transfer and a target registry.
* Storage accounts: Create source and target storage accounts that ACR Transfer will use to upload and download registry artifacts. Create a blob container for artifact transfer in each account.
* Key Vaults: Key Vaults are used to store SAS token secrets for export and import. This sample assumes the key vault is in the same resource group as the registry.
* User Assigned Identities (optional): You may create user assigned managed identity resources to assign to each pipeline [see tutorial](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-cli). Otherwise, we will automatically create a system assigned identity for the exportPipeline and importPipeline. You do not need to perform RBAC on these managed identities prior to creating the pipelines. The sample will give either a system or user assigned managed identity access to get secrets from the key vault.

Note: The sample assumes the above resources are all in the same subscription.

Use the above info to fill out both ExportPipeline and ImportPipeline sections of appsettings.json, as well as the desired pipeline resource name.
```
    "ResourceGroupName": "",
    "RegistryName": "",
    "PipelineName": "",
    "KeyVaultUri": "",
    "ContainerUri": "",
    "UserAssignedIdentity": "",
    "Options": []
```

For more info on `Options` please refer to [Export options](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images#export-options) and [Import options](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images#import-options).

* If you intent to run the exportPipeline created in this sample, please fill out the following section of appsettings.json

```
"ExportPipelineRun": {
    "PipelineRunName": "",
    "TargetName": "",
    "Artifacts": []
  }
```

Where `TargetName` is the name you choose for the artifacts blob exported to your source storage account, such as 'myblob'.
And `Artifacts` is the list of up to 50 artifacts that you would like to transfer from your source registry.
`Example: [samples/hello-world:v1", "samples/nginx:v1" , "myrepository@sha256:0a2e01852872..."]`





* Build DotNetTransfer.csproj (DotNetCore SDK 3.1 required)
```sh
dotnet build DotNetTransfer/DotNetTransfer.csproj

dotnet DotNetTransfer/bin/Debug/netcoreapp3.1/DotNetTransfer.dll
```

## More information ##

[https://github.com/Azure/azure-sdk-for-net](https://github.com/Azure/azure-sdk-for-net)

If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212).