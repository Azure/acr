# **Overview of Registry Artifact Transfer Tool** ##

The Registry Artifact Transfer Tool supports transfer workflows that combines both registry artifact import and export. For example, in a single run, it can (1) import artifacts from a source registry as well as storage blobs previously exported by ACR transfer into a target ACR; (2) batch export the artifacts imported in (1) along with any additional artifacts existing in the target ACR into storage blobs; (3) copy the blobs exported in (2) to a destination blob container so that they are ready to be imported to a different ACR.

Please see the public documentation for more details on ACR [Transfer](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images) and [Image Import](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-import-images).

### **Features**

1. Import  
  a. Import registry artifacts from a source registry to a target ACR.  
  b. Import registry artifacts from storage blobs exported by ACR transfer to a target ACR.

2. Export  
  a. Export registry artifacts from a target ACR into storage blobs.  
  b. Optionally, copy the exported storage blobs to another blob container. The copy destination blob container can be hooked up with a different ACR with an import pipeline to effectively transfer the artifacts to the latter, with storage blobs as transfer mediums.


# **Prerequisities**

0. **Common**  
  * An existing target ACR. The ACR must be in [Premium](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-skus) SKU if feature 1b, 2a, or 2b will be used.
  * A service principle with contributor role of the target ACR.

1. **Import**  
    a. Import registry artifacts from a source registry to a target ACR.  
    * **Source container registry**: an existing source container registry with artifacts to transfer. To permit image import from the source registry:
      * If the source registry is an ACR, either the service principle in the common prerequisities has read permission (e.g. Reader role) to the source ACR or the source ACR's user name and password are available. The user name can be the admin user of the source ACR or the client ID of a service principle with read permission (and client secret as the password). More details on the permission requirement can be found in [this](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-import-images#import-from-another-azure-container-registry) section.
      * If the source registry is a non-Azure private registry, credentials that enable pull access to the registry need to be available.

    b. Import registry artifacts from storage blobs exported by ACR transfer to a target ACR.
    * **Source storage blobs**: existing source storage blobs in storage containers exported by ACR [export pipelines](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images#create-exportpipeline-with-resource-manager) where the source registry artifacts are contained.
    * **Import pipeline**: an existing [import pipeline](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images#create-importpipeline-with-resource-manager) under the target ACR with valid permission (managed identity and sas token in a key vault) to access the storage blob container where the source blobs are located.

2. **Export**  
    a. Export registry artifacts from a target ACR to storage blobs.
    * **Registry artifacts**: existing registry artifacts to be exported from the target ACR.
    * **Export pipeline**: an existing [export pipeline](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-transfer-images#create-exportpipeline-with-resource-manager) under the target ACR with valid permission (managed identity and sas token in a key vault) to access the storage blob container where the target blobs will be exported.

    b. Copy the exported storage blobs to another blob container.
    * **Copy destination blob container**: an existing storage blob container where the exported blobs will be copied to.
    * **SAS tokens**: A SAS token with read access to the export blob container and a SAS token with write access to the copy destination blob container.


# **Configurations** ##

The tool uses a single JSON formatted configuration file. The default configuration file name is `transferdefinition.json` and a sample can be found in `src/transferdefinition.json`.

### **Common configurations**

The common configurations consist of the information about the Azure environment, the target ACR, and the service principle used to access the target ACR.

```
  "AzureEnvironment": {
    "Name": "AzureGlobalCloud"
  },
  "Registry": {
    "TenantId": "myTenantId",
    "SubscriptionId": "mySubscriptionId",
    "ResourceGroupName": "myResourceGroupName",
    "Name": "myRegistryName"
  },
  "Identity": {
    "ClientId": "myClientId",
    "ClientSecret": "myClientSecret"
  },
```

| Configuration               | Description                                                               |
|-----------------------------|---------------------------------------------------------------------------|
| AzureEnvironment/Name       | The name of the Azure environment. The list of the supported names can be found in [this](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.resourcemanager.fluent.azureenvironment?view=azure-dotnet) document. |
| Registry/TenantId           | The tenant ID of the target ACR.                                          |
| Registry/SubscriptionId     | The subscription ID of the target ACR.                                    |
| Registry/ResourceGroupName  | The resource group name of the target ACR.                                |
| Registry/Name               | The name of the target ACR.                                               |
| Identity/ClientId           | The client ID of the service principle used to access the target ACR.     |
| Identity/ClientSecret       | The client secret of the service principle.                               |


### **Import configurations**

The import configurations specify:
* The repositories and tags from a source registry to be imported to the target ACR.
* The storage blobs to be imported to the target ACR as well as the import pipeline to be used for the import.

```
  "Import": {
    "Enabled": true,
    "Force": true,
    "SourceRegistry": {
      "ResourceId": "mySourceRegistryResourceId",
      "RegistryUri": "mysourceregistry.azurecr.io",
      "UserName": "clientId-or-adminUser",
      "Password": "clientSecret-or-adminPassword"
    },
    "Repositories": [
      "sourceRepo",
      "sourceRepoPrefix*"
    ],
    "Tags": [
      "sourceRepo1:tag1",
      "sourceRepo2:tag2"
    ],
    "ImportPipelineName": "myImportPipelineName",
    "Blobs": [
      "exportedBlob1",
      "exportedBlob2",
      "exportedBlob3"
    ]
  },
```

| Configuration                     | Features  | Description |
|-----------------------------------|-----------|--------------------------------------------------------------------------|
| Import/Enabled                    | 1a, 1b    | Configures Whether the import feature is enabled. The default value is `false`. |
| Import/Force                      | 1a        | Configures whether any existing target tags will be overwritten (see [ImportMode](https://docs.microsoft.com/en-us/rest/api/containerregistry/registries/importimage#importmode)). The default value is `true`.|
| Import/SourceRegistry/ResourceId  | 1a        | The Azure resource ID of the source registry if it is an ACR. If `ResourceId` is specified, `RegistryUri`, `UserName`, and `Password` must be skipped, and vice versa. The `Identity/ClientId` will be used to access the source ACR. |
| Import/SourceRegistry/RegistryUri | 1a        | The login server of the source registry, applicable to both ACRs and non-Azure public and private registries. If the source registry is a public registry, `UserName` and `Password` must be skipped. |
| Import/SourceRegistry/UserName    | 1a        | The user name to access the source registry.                              |
| Import/SourceRegistry/Password    | 1a        | The password of the user name.                                            |
| Import/Repositories               | 1a        | The list of repository names and name prefixes to match the repositories in the source registry. All tags in the matched repositories are imported. A repository name prefix can be specified with a `*` after the prefix string |
| Import/Tags                       | 1a        | The list of tags in the source registry to import.                        |
| Import/ImportPipelineName         | 1b        | The import pipeline used to import source blobs to the target ACR.        |
| Import/Blobs                      | 1b        | The source blobs containing the source registry artifacts, previously exported by ACR export pipelines. All blobs must be in the blob container targeted by the specified import pipeline. |

### **Export configurations**

The export configurations specify:
* The export controls such as batch artifact count and blob name prefix.
* The repositories and tags from the target ACR to be exported to storage blobs.
* The blob copy SAS token and URI for the export target blob container and the copy destination blob container, respectively.

```
  "Export": {
    "Enabled": true,
    "ExportPipelineName": "myExportPipelineName",
    "MaxArtifactCountPerBlob": 50,
    "BlobNamePrefix": "artifacts",
    "IncludeImportedArtifacts": true,
    "Repositories": [
      "targetRepo",
      "targetRepoPrefix*"
    ],
    "Tags": [
      "targetRepo1:tag1",
      "targetRepo2:tag2"
    ],
    "CopyBlobs": {
      "Enabled": true,
      "SourceSasToken": "sourceBlobContainerSasToken",
      "DestContainerSasUri": "destinationBlobContainerSasUri"
    }
  }
```

| Configuration                         | Features  | Description |
|---------------------------------------|-----------|---------------------------------------------------------------------------  |
| Export/Enabled                        | 2a        | Configures whether the export feature is enabled. The default value is `false`. |
| Export/ExportPipelineName             | 2a        | The export pipeline under the target ACR to export the specified artifacts. |
| Export/MaxArtifactCountPerBlob        | 2a        | The maximum batch export artifact count. The default value is `50`.         |
| Export/BlobNamePrefix                 | 2a        | The name prefix of the export storage blobs.                                |
| Export/IncludeImportedArtifacts       | 2a        | Configures whether the artifacts imported by 1a and 1b are included in the export. The default value is `false`. |
| Export/Repositories                   | 2a        | The list of repository names and name prefixes to match the repositories in the target ACR. All tags in the matched repositories are exported. A repository name prefix can be specified with a `*` after the prefix string. |
| Export/Tags                           | 2a        | The list of tags in the target ACR to export.                               |
| Export/CopyBlobs/Enabled              | 2b        | Configures whether the blob copy feature is enabled.                        |
| Export/CopyBlobs/SourceSasToken       | 2b        | The SAS token of the blob container targed by the specified export pipeline.|
| Export/CopyBlobs/DestContainerSasUri  | 2b        | The SAS URI of the copy destination blob container.                         |


# **Running the tool** ##
* Build the project (.NET Core SDK 3.1 required)
```
dotnet build src/RegistryArtifactTransfer.csproj
```

* Run the tool with the default configuration file transferdefinition.json
```
cd src/bin/Debug/netcoreapp3.1
dotnet RegistryArtifactTransfer.dll
```

* Run the tool with a custom configuration file name  
Copy the configuration file to src/bin/Debug/netcoreapp3.1 and run
```
cd src/bin/Debug/netcoreapp3.1
dotnet RegistryArtifactTransfer.dll myTransferDefinition.json
```
