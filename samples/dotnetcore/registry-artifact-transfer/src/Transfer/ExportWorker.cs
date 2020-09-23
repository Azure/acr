using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryArtifactTransfer
{
    public class ExportWorker
    {
        private readonly ExportConfiguration _exportConfiguration;
        private readonly IdentityConfiguration _identityConfiguration;
        private readonly ILogger _logger;
        private readonly TransferClient _transferClient;
        private readonly Registry _registry;

        public ExportWorker(
            ExportConfiguration exportDefinition,
            RegistryConfiguration registryConfiguration,
            IdentityConfiguration identityConfiguration,
            TransferClient transferClient,
            ILogger<ExportWorker> logger)
        {
            _transferClient = transferClient ?? throw new ArgumentNullException(nameof(transferClient));
            _exportConfiguration = exportDefinition ?? throw new ArgumentNullException(nameof(exportDefinition));
            _identityConfiguration = identityConfiguration ?? throw new ArgumentNullException(nameof(identityConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _registry = new Registry(
                registryConfiguration.TenantId,
                registryConfiguration.SubscriptionId,
                registryConfiguration.ResourceGroupName,
                registryConfiguration.Name);
        }

        public async Task RunAsync(TransferReport transferReport)
        {
            if (!_exportConfiguration.Enabled)
            {
                return;
            }

            //
            // Include imported images
            var importedImages = new List<string>();
            if (_exportConfiguration.IncludeImportedArtifacts && transferReport.ImportArtifacts.Succeeded.Count > 0)
            {
                importedImages.AddRange(transferReport.ImportArtifacts.Succeeded);
            }

            if (importedImages.Count == 0 &&
                _exportConfiguration.Repositories.Count == 0 &&
                _exportConfiguration.Tags.Count == 0)
            {
                return;
            }

            //
            // Validate exportPipeline
            var exportPipeline = await _transferClient.GetExportPipelineAsync(_exportConfiguration.ExportPipelineName).ConfigureAwait(false);
            if (!exportPipeline.ProvisioningState.Equals("succeeded", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"ExportPipeline:{_exportConfiguration.ExportPipelineName} is in non-success provisioning state {exportPipeline.ProvisioningState}.");
            }

            //
            // Export images
            var exportJobs = await CreateExportJobsAsync(importedImages).ConfigureAwait(false);

            await exportJobs.ThrottledWhenAll(
                async (job) => await ExecuteExportAsync(job).ConfigureAwait(false),
                _exportConfiguration.MaxConcurrency).ConfigureAwait(false);

            foreach (var job in exportJobs)
            {
                if (job.Status == TransferJobStatus.Succeeded)
                {
                    transferReport.ExportArtifacts.Succeeded.AddRange(job.Images);
                    transferReport.ExportBlobs.Succeeded.Add(job.ExportBlobName);
                }
                else if (job.Status == TransferJobStatus.Failed)
                {
                    transferReport.ExportArtifacts.Failed.AddRange(job.Images);
                    transferReport.ExportBlobs.Failed.Add(job.ExportBlobName);
                }
            }

            //
            // Copy exported blobs
            if (_exportConfiguration.CopyBlobs != null &&
                _exportConfiguration.CopyBlobs.Enabled &&
                transferReport.ExportBlobs.Succeeded.Count > 0)
            {
                var sourceContainerSas = new Uri(await GetSourceSasUriAsync().ConfigureAwait(false));
                var targetContainerSas = new Uri(_exportConfiguration.CopyBlobs.DestContainerSasUri);
                var blobCopier = new BlobCopier(sourceContainerSas, targetContainerSas, _logger);

                foreach (var blobName in transferReport.ExportBlobs.Succeeded)
                {
                    _logger.LogInformation($"Blob {blobName}: starting to copy .");

                    try
                    {
                        await blobCopier.CopyAsync(blobName).ConfigureAwait(false);
                        transferReport.CopyBlobs.Succeeded.Add(blobName);
                        _logger.LogInformation($"Blob {blobName}: Successfully copied.");
                    }
                    catch (Exception e)
                    {
                        transferReport.CopyBlobs.Failed.Add(blobName);
                        _logger.LogError($"Blob {blobName}: failed to copy, exception: {e}.");
                    }
                }
            }
        }

        private async Task ExecuteExportAsync(ExportJob exportJob)
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_exportConfiguration.TransferTimeoutInSeconds)))
                {
                    await _transferClient.ExportImagesToStorageAsync(
                        exportJob.ExportPipelineName,
                        exportJob.PipelineRunName,
                        exportJob.Images,
                        exportJob.ExportBlobName,
                        cts.Token).ConfigureAwait(false);
                }

                exportJob.Status = TransferJobStatus.Succeeded;
                _logger.LogInformation($"PipelineRun {exportJob.PipelineRunName} succeeded.");
            }
            catch (Exception e)
            {
                exportJob.Status = TransferJobStatus.Failed;
                _logger.LogError($"PipelineRun {exportJob.PipelineRunName} failed, exception: {e}.");
            }
        }

        private async Task<List<ExportJob>> CreateExportJobsAsync(List<string> importedImages)
        {
            var exportJobs = new List<ExportJob>();
            var images = new List<string>();

            var importedImageCount = importedImages?.Count ?? 0;
            if (importedImageCount > 0)
            {
                images.AddRange(importedImages);
            }

            if (_exportConfiguration.Repositories != null && _exportConfiguration.Repositories.Count > 0)
            {
                var artifactProvider = new ArtifactProvider(_logger);
                var loginServer = await _transferClient.GetRegistryLoginServerAsync().ConfigureAwait(false);

                var matchedTags = await artifactProvider.GetArtifactsAsync(
                    loginServer,
                    _identityConfiguration.ClientId,
                    _identityConfiguration.ClientSecret,
                    _exportConfiguration.Repositories).ConfigureAwait(false);
                
                images.AddRange(matchedTags);
            }

            if (_exportConfiguration.Tags != null)
            {
                images.AddRange(_exportConfiguration.Tags);
            }

            _logger.LogInformation($"Total artifacts to export: {images.Count}.");

            var i = 0;
            while (i < images.Count)
            {
                if (i % _exportConfiguration.MaxArtifactCountPerBlob == 0)
                {
                    var endIndex = Math.Min(i + _exportConfiguration.MaxArtifactCountPerBlob - 1, images.Count - 1);
                    var pipelineRunName = Guid.NewGuid().ToString("N");
                    var targetBlobName = $"{_exportConfiguration.BlobNamePrefix}From{i+1}To{endIndex+1}";

                    var job = new ExportJob
                    {
                        PipelineRunName = pipelineRunName,
                        ExportPipelineName = _exportConfiguration.ExportPipelineName,
                        ExportBlobName = targetBlobName
                    };

                    while (i <= endIndex)
                    {
                        job.Images.Add(images[i]);
                        i++;
                    }

                    exportJobs.Add(job);
                }
            }

            return exportJobs;
        }

        private async Task<string> GetSourceSasUriAsync()
        {
            var exportPipeline = await _transferClient.GetExportPipelineAsync(_exportConfiguration.ExportPipelineName).ConfigureAwait(false);
            var blobContainerUri = exportPipeline?.Target?.Uri;

            if (string.IsNullOrWhiteSpace(blobContainerUri))
            {
                throw new Exception($"Invalid source blob container URI from export pipeline {exportPipeline}.");
            }

            var sasToken = _exportConfiguration.CopyBlobs.SourceSasToken;
            if (!sasToken.StartsWith('?'))
            {
                sasToken = "?" + sasToken;
            }

            return blobContainerUri + sasToken;
        }
    }
}
