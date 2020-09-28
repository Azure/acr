using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Management.ContainerRegistry.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Collections.Generic;
using System.Threading;
using static RegistryArtifactTransfer.ResourceId;

namespace RegistryArtifactTransfer
{
    public class TransferClient
    {
        private const string ImportModeForce = "Force";
        private const string ImportModeNoForce = "NoForce";
        private readonly ContainerRegistryManagementClient _registryClient;
        private readonly RegistryConfiguration _registryConfiguration;

        public TransferClient(
            AzureEnvironmentConfiguration azureEnvironmentConfiguration,
            IdentityConfiguration identityConfiguration,
            RegistryConfiguration registryConfiguration)
        {
            _registryConfiguration = registryConfiguration ?? throw new ArgumentNullException(nameof(registryConfiguration));

            var env = AzureEnvironment.FromName(azureEnvironmentConfiguration.Name);
            if (env == null)
            {
                env = new AzureEnvironment
                {
                    Name = azureEnvironmentConfiguration.Name,
                    AuthenticationEndpoint = azureEnvironmentConfiguration.AuthenticationEndpoint,
                    ManagementEndpoint = azureEnvironmentConfiguration.ManagementEndpoint,
                    ResourceManagerEndpoint = azureEnvironmentConfiguration.ResourceManagerEndpoint,
                    GraphEndpoint = azureEnvironmentConfiguration.GraphEndpoint,
                    KeyVaultSuffix = azureEnvironmentConfiguration.KeyVaultSuffix,
                    StorageEndpointSuffix = azureEnvironmentConfiguration.StorageEndpointSuffix
                };
            }

            var credential = new AzureCredentials(
                new ServicePrincipalLoginInformation
                {
                    ClientId = identityConfiguration.ClientId,
                    ClientSecret = identityConfiguration.ClientSecret
                },
                registryConfiguration.TenantId,
                env);

            _registryClient = new ContainerRegistryManagementClient(credential.WithDefaultSubscription(registryConfiguration.SubscriptionId));
            _registryClient.SubscriptionId = registryConfiguration.SubscriptionId;
        }

        public async System.Threading.Tasks.Task ImportImageAsync(
            Registry sourceRegistry,
            string sourceImage,
            bool force = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sourceRegistry == null)
            {
                throw new ArgumentNullException(nameof(sourceRegistry));
            }

            var sourceResourceId = sourceRegistry.ResourceId?.ToString();
            var sourceLoginServer = string.IsNullOrEmpty(sourceResourceId) ? sourceRegistry.LoginServer : null;
            var importSource = new ImportSource()
            {
                ResourceId = sourceResourceId,
                RegistryUri = sourceLoginServer,
                SourceImage = sourceImage
            };

            if (!string.IsNullOrEmpty(sourceRegistry.UserName))
            {
                importSource.Credentials = new ImportSourceCredentials()
                {
                    Username = sourceRegistry.UserName,
                    Password = sourceRegistry.Password
                };
            }

            var importImageParameters = new ImportImageParameters()
            {
                Mode = force ? ImportModeForce : ImportModeNoForce,
                Source = importSource,
                TargetTags = new List<string>{sourceImage}
            };

            importImageParameters.Validate();

            await _registryClient.Registries.ImportImageAsync(
                _registryConfiguration.ResourceGroupName,
                _registryConfiguration.Name,
                importImageParameters,
                cancellationToken).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task<IList<string>> ImportImagesFromStorageAsync(
            string importPipelineName,
            string pipelineRunName,
            string sourceBlobName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = CreateImportPipelineRunRequest(importPipelineName, sourceBlobName);
            var pipelineRun = await CreatePipelineRunAsync(pipelineRunName, request, cancellationToken).ConfigureAwait(false);
            return pipelineRun?.Response?.ImportedArtifacts;
        }

        public async System.Threading.Tasks.Task ExportImagesToStorageAsync(
            string exportPipelineName,
            string pipelineRunName,
            List<string> images,
            string targetBlobName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = CreateExportPipelineRunRequest(exportPipelineName, images, targetBlobName);
            await CreatePipelineRunAsync(pipelineRunName, request, cancellationToken).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task<ExportPipeline> GetExportPipelineAsync(
            string exportPipelineName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _registryClient.ExportPipelines.GetAsync(
                _registryConfiguration.ResourceGroupName,
                _registryConfiguration.Name,
                exportPipelineName,
                cancellationToken).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task<ImportPipeline> GetImportPipelineAsync(
            string importPipelineName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _registryClient.ImportPipelines.GetAsync(
                _registryConfiguration.ResourceGroupName,
                _registryConfiguration.Name,
                importPipelineName,
                cancellationToken).ConfigureAwait(false);
        }

        public async System.Threading.Tasks.Task<string> GetRegistryLoginServerAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var registry = await _registryClient.Registries.GetAsync(
                _registryConfiguration.ResourceGroupName,
                _registryConfiguration.Name,
                cancellationToken).ConfigureAwait(false);
            
            return registry?.LoginServer;
        }

        private async System.Threading.Tasks.Task<PipelineRun> CreatePipelineRunAsync(
            string pipelineRunName,
            PipelineRunRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _registryClient.PipelineRuns.CreateAsync(
                _registryConfiguration.ResourceGroupName,
                _registryConfiguration.Name,
                pipelineRunName,
                request,
                forceUpdateTag: null,
                cancellationToken).ConfigureAwait(false);
        }

        private PipelineRunRequest CreateImportPipelineRunRequest(
            string importPipelineName,
            string sourceBlobName)
        {
            var importPipelineResourceId = new ResourceId(
                _registryConfiguration.SubscriptionId,
                _registryConfiguration.ResourceGroupName,
                _registryConfiguration.Name,
                RegistriesARMResourceType,
                ImportPipelineResourceType,
                importPipelineName);

            return new PipelineRunRequest
            {
                PipelineResourceId = importPipelineResourceId.ToString(),
                Source = new PipelineRunSourceProperties
                {
                    Type = PipelineRunTargetType.AzureStorageBlob.ToString(),
                    Name = sourceBlobName
                }
            };
        }

        private PipelineRunRequest CreateExportPipelineRunRequest(
            string exportPipelineName,
            List<string> images,
            string targetBlobName)
        {
            var exportPipelineResourceId = new ResourceId(
                _registryConfiguration.SubscriptionId,
                _registryConfiguration.ResourceGroupName,
                _registryConfiguration.Name,
                RegistriesARMResourceType,
                ExportPipelineResourceType,
                exportPipelineName);

            return new PipelineRunRequest
            {
                PipelineResourceId = exportPipelineResourceId.ToString(),
                Artifacts = images,
                Target = new PipelineRunTargetProperties
                {
                    Type = PipelineRunTargetType.AzureStorageBlob.ToString(),
                    Name = targetBlobName
                }
            };
        }
    }
}
