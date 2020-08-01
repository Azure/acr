using ContainerRegistryTransfer.Helpers;
using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Management.ContainerRegistry.Models;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Extensions.Configuration;
using System;


namespace ContainerRegistryTransfer.Clients
{
    public class ImportClient
    {
        ContainerRegistryManagementClient registryClient;
        KeyVaultManagementClient keyVaultClient;
        Options options;

        public ImportClient(ContainerRegistryManagementClient registryClient, KeyVaultManagementClient keyVaultClient, Options options)
        {
            this.registryClient = registryClient;
            this.keyVaultClient = keyVaultClient;
            this.options = options;
        }

        public ImportPipeline CreateImportPipeline()
        {
            Console.WriteLine($"Creating importPipeline {options.ImportPipeline.PipelineName}.");
            var importPipeline = CreateImportPipelineResource();
            Console.WriteLine($"Successfully created importPipeline {options.ImportPipeline.PipelineName}.");

            var vaultName = KeyVaultHelper.GetKVNameFromUri(options.ImportPipeline.KeyVaultUri);

            Console.WriteLine($"Adding an accessPolicy for importPipeline {options.ImportPipeline.PipelineName} to vault {vaultName}.");
            
            // give the pipeline identity access to the key vault
            KeyVaultHelper.AddKeyVaultAccessPolicy(
                keyVaultClient,
                options.TenantId,
                options.SubscriptionId,
                options.ExportPipeline.ResourceGroupName,
                vaultName,
                IdentityHelper.GetManagedIdentityPrincipalId(importPipeline.Identity));

            return importPipeline;
        }

        public ImportPipeline CreateImportPipelineResource()
        {
            var importResourceGroupName = options.ImportPipeline.ResourceGroupName;
            var importRegistryName = options.ImportPipeline.RegistryName;

            var registry = registryClient.Registries.Get(
                importResourceGroupName,
                importRegistryName);


            if (registry != null)
            {
                var importPipeline = new ImportPipeline(
                name: options.ImportPipeline.PipelineName,
                location: registry.Location,
                identity: IdentityHelper.GetManagedIdentity(options.ImportPipeline.UserAssignedIdentity),
                source: new ImportPipelineSourceProperties
                {
                    Type = "AzureStorageBlobContainer",
                    Uri = options.ImportPipeline.ContainerUri,
                    KeyVaultUri = options.ImportPipeline.KeyVaultUri
                },
                trigger: new PipelineTriggerProperties
                {
                    SourceTrigger = new PipelineSourceTriggerProperties
                    {
                        Status = "Enabled"
                    }
                },
                options: options.ImportPipeline.Options
                );

                return registryClient.ImportPipelines.Create(registryName: registry.Name,
                                                                resourceGroupName: options.ImportPipeline.ResourceGroupName,
                                                                importPipelineName: options.ImportPipeline.PipelineName,
                                                                importPipelineCreateParameters: importPipeline);
            }
            else
            {
                throw new ArgumentException($"Could not find registry '{importRegistryName}'. Please ensure the registry exists in the current resource group {importResourceGroupName}.");
            }
        }
    }
}
