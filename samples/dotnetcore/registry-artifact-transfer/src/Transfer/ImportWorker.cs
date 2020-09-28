using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryArtifactTransfer
{
    public class ImportWorker
    {
        private readonly ImportConfiguration _importConfiguration;
        private readonly ILogger _logger;
        private readonly TransferClient _transferClient;
        private readonly Registry _sourceRegistry;

        public ImportWorker(
            ImportConfiguration importConfiguration,
            RegistryConfiguration registryConfiguration,
            TransferClient transferClient,
            ILogger<ImportWorker> logger)
        {
            if (registryConfiguration == null)
            {
                throw new ArgumentNullException(nameof(registryConfiguration));
            }

            _importConfiguration = importConfiguration ?? throw new ArgumentNullException(nameof(importConfiguration));
            _transferClient = transferClient ?? throw new ArgumentNullException(nameof(transferClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!string.IsNullOrWhiteSpace(importConfiguration.SourceRegistry.ResourceId))
            {
                var resourceId = ResourceId.Parse(importConfiguration.SourceRegistry.ResourceId);
                _sourceRegistry = new Registry(
                    null,
                    resourceId.SubscriptionId,
                    resourceId.ResourceGroupName,
                    resourceId.ResourceName);
            }
            else
            {
                _sourceRegistry = new Registry(
                    importConfiguration.SourceRegistry.RegistryUri,
                    importConfiguration.SourceRegistry.UserName,
                    importConfiguration.SourceRegistry.Password);
            }
        }

        public async Task RunAsync(TransferReport transferReport)
        {
            if (!_importConfiguration.Enabled)
            {
                return;
            }

            //
            // Validate importPipeline
            if (_importConfiguration.Blobs.Count > 0)
            {
                var importPipeline = await _transferClient.GetImportPipelineAsync(_importConfiguration.ImportPipelineName).ConfigureAwait(false);
                if (!importPipeline.ProvisioningState.Equals("succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"ImportPipeline:{_importConfiguration.ImportPipelineName} is in non-success provisioning state {importPipeline.ProvisioningState}.");
                }
            }

            var importJobs = await CreateImportJobs().ConfigureAwait(false);

            await importJobs.ThrottledWhenAll(
                async (job) => await ExecuteAsync(job).ConfigureAwait(false),
                _importConfiguration.MaxConcurrency).ConfigureAwait(false);

            foreach (var job in importJobs)
            {
                if (job.Status == TransferJobStatus.Succeeded)
                {
                    transferReport.ImportArtifacts.Succeeded.AddRange(job.Images);

                    if (job.SourceType == ImportSourceType.AzureStorageBlob)
                    {
                        transferReport.ImportBlobs.Succeeded.Add(job.ImportBlobName);
                    }
                }
                else if (job.Status == TransferJobStatus.Failed)
                {
                    transferReport.ImportArtifacts.Failed.AddRange(job.Images);

                    if (job.SourceType == ImportSourceType.AzureStorageBlob)
                    {
                        transferReport.ImportBlobs.Failed.Add(job.ImportBlobName);
                    }
                }
            }
        }

        private async Task<List<ImportJob>> CreateImportJobs()
        {
            var importJobs = new List<ImportJob>();
            var images = new List<string>();

            if (_importConfiguration.Repositories != null)
            {
                var artifactProvider = new ArtifactProvider(_logger);

                var matchedTags = await artifactProvider.GetArtifactsAsync(
                    _importConfiguration.SourceRegistry.RegistryUri,
                    _importConfiguration.SourceRegistry.UserName,
                    _importConfiguration.SourceRegistry.Password,
                    _importConfiguration.Repositories).ConfigureAwait(false);

                images.AddRange(matchedTags);
            }

            if (_importConfiguration.Tags != null)
            {
                images.AddRange(_importConfiguration.Tags);
            }

            _logger.LogInformation($"Total registry artifacts to import: {images.Count}");

            foreach (var image in images)
            {
                var job = new ImportJob
                {
                    SourceType = ImportSourceType.RegistryImage,
                };

                job.Images.Add(image);
                importJobs.Add(job);
            }

            var blobs = _importConfiguration.Blobs;
            _logger.LogInformation($"Total storage blobs to import: {blobs.Count}");

            if (blobs != null && blobs.Count > 0)
            {
                foreach (var blob in blobs)
                {
                    var job = new ImportJob
                    {
                        SourceType = ImportSourceType.AzureStorageBlob,
                        ImportPipelineName = _importConfiguration.ImportPipelineName,
                        PipelineRunName = Guid.NewGuid().ToString("N"),
                        ImportBlobName = blob
                    };

                    importJobs.Add(job);
                }
            }

            return importJobs;
        }

        private async Task ExecuteAsync(ImportJob importJob)
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_importConfiguration.TransferTimeoutInSeconds)))
            {
                if (importJob.SourceType == ImportSourceType.RegistryImage)
                {
                    await ExecuteImportImageAsync(importJob, cts.Token).ConfigureAwait(false);
                }
                else if (importJob.SourceType == ImportSourceType.AzureStorageBlob)
                {
                    await ExecutePipelineRunAsync(importJob, cts.Token).ConfigureAwait(false);
                }
            }
        }

        private async Task ExecuteImportImageAsync(ImportJob importJob, CancellationToken cancellationToken)
        {
            var image = importJob.Images.First();

            try
            {
                await _transferClient.ImportImageAsync(
                    _sourceRegistry,
                    image,
                    _importConfiguration.Force,
                    cancellationToken).ConfigureAwait(false);

                importJob.Status = TransferJobStatus.Succeeded;
                _logger.LogInformation($"Sucessfully imported {image}");
            }
            catch (Exception e)
            {
                importJob.Status = TransferJobStatus.Failed;
                _logger.LogError($"Failed to import: {image}, exception: {e}");
            }
        }

        private async Task ExecutePipelineRunAsync(ImportJob importJob, CancellationToken cancellationToken)
        {
            try
            {
                var importedImages = await _transferClient.ImportImagesFromStorageAsync(
                    importJob.ImportPipelineName,
                    importJob.PipelineRunName,
                    importJob.ImportBlobName,
                    cancellationToken).ConfigureAwait(false);

                importJob.Status = TransferJobStatus.Succeeded;

                if (importedImages != null && importedImages.Count > 0)
                {
                    foreach (var image in importedImages)
                    {
                        importJob.Images.Add(image);
                        _logger.LogInformation($"Sucessfully imported {image}, pipelineRun:{importJob.PipelineRunName}, blob:{importJob.ImportBlobName}.");
                    }
                }
            }
            catch (Exception e)
            {
                importJob.Status = TransferJobStatus.Failed;
                _logger.LogError($"Failed to import blob:{importJob.ImportBlobName}, pipelineRun:{importJob.PipelineRunName}, exception: {e}");
            }
        }
    }
}
