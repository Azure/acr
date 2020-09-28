using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryArtifactTransfer
{
    public class BlobCopier
    {
        private readonly ILogger _logger;
        private readonly CloudBlobContainer _sourceContainer;
        private readonly CloudBlobContainer _targetContainer;

        public BlobCopier(
            Uri sourceContainerSas,
            Uri targetContainerSas,
            ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sourceContainer = new CloudBlobContainer(sourceContainerSas);
            _targetContainer = new CloudBlobContainer(targetContainerSas);
        }

        public async Task CopyAsync(
            string blobName,
            CancellationToken token = default(CancellationToken))
        {
            var sourceBlob = _sourceContainer.GetBlobReference(blobName);
            var targetBlob = _targetContainer.GetBlobReference(blobName);
            TransferCheckpoint checkpoint = null;
            SingleTransferContext context = GetSingleTransferContext(checkpoint, blobName);

            await TransferManager.CopyAsync(
                sourceBlob: sourceBlob,
                destBlob: targetBlob,
                copyMethod: CopyMethod.ServiceSideAsyncCopy,
                options: null,
                context: context,
                cancellationToken: token).ConfigureAwait(false);
        }

        private SingleTransferContext GetSingleTransferContext(
            TransferCheckpoint checkpoint,
            string blobName)
        {
            SingleTransferContext context = new SingleTransferContext(checkpoint);

            context.ShouldOverwriteCallbackAsync = TransferContext.ForceOverwrite;

            context.ProgressHandler = new Progress<TransferStatus>((progress) =>
            {
                _logger.LogInformation($"{blobName}: bytes transferred {progress.BytesTransferred}.");
            });

            return context;
        }
    }
}