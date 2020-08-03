using ContainerRegistryTransfer.Helpers;
using ContainerRegistryTransfer.Models;
using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Management.ContainerRegistry.Models;
using Microsoft.Azure.Management.KeyVault;
using System;
using System.Threading.Tasks;

namespace ContainerRegistryTransfer.Clients
{
    internal class ImportClient
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

        public async Task<ImportPipeline> CreateImportPipelineAsync()
        {
            var importPipelineName = options.ImportPipeline.PipelineName;
            Console.WriteLine($"Creating importPipeline {importPipelineName}.");
            var importPipeline = await CreateImportPipelineResourceAsync().ConfigureAwait(false);
            Console.WriteLine($"Successfully created importPipeline {importPipelineName}.");
            
            // give the pipeline identity access to the key vault
            await KeyVaultHelper.AddKeyVaultAccessPolicyAsync(
                keyVaultClient,
                importPipelineName,
                options.TenantId,
                options.ImportPipeline.ResourceGroupName,
                options.ImportPipeline.KeyVaultUri,
                IdentityHelper.GetManagedIdentityPrincipalId(importPipeline.Identity));

            return importPipeline;
        }

        public async Task<ImportPipeline> CreateImportPipelineResourceAsync()
        {
            var importResourceGroupName = options.ImportPipeline.ResourceGroupName;
            var importRegistryName = options.ImportPipeline.RegistryName;

            var registry = await registryClient.Registries.GetAsync(
                importResourceGroupName,
                importRegistryName).ConfigureAwait(false);

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

                return await registryClient.ImportPipelines.CreateAsync(registryName: registry.Name,
                                                                resourceGroupName: options.ImportPipeline.ResourceGroupName,
                                                                importPipelineName: options.ImportPipeline.PipelineName,
                                                                importPipelineCreateParameters: importPipeline).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException($"Could not find registry '{importRegistryName}'. Please ensure the registry exists in the current resource group {importResourceGroupName}.");
            }
        }
    }
}
