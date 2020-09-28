using System;
using System.Collections.Generic;

namespace RegistryArtifactTransfer
{
    public class ExportConfiguration
    {
        public bool Enabled { get; set; }
        public bool IncludeImportedArtifacts { get; set; }
        public string ExportPipelineName { get; set; }
        public string BlobNamePrefix { get; set; }
        public int MaxConcurrency { get; set; } = 50;
        public int MaxArtifactCountPerBlob { get; set; } = 50;
        public int TransferTimeoutInSeconds { get; set; } = 300;
        public List<string> Repositories { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public CopyBlobsConfiguration CopyBlobs { get; set; }

        public void Validate()
        {
            if (!Enabled)
            {
                return;
            }

            if (MaxConcurrency < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(MaxConcurrency), MaxConcurrency, "must be larger than 0");
            }

            if (MaxArtifactCountPerBlob < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(MaxArtifactCountPerBlob), MaxArtifactCountPerBlob, "must be larger than 0");
            }

            if (TransferTimeoutInSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(TransferTimeoutInSeconds), TransferTimeoutInSeconds, "must be larger than 0");
            }

            if (string.IsNullOrWhiteSpace(ExportPipelineName))
            {
                throw new ArgumentNullException(nameof(ExportPipelineName));
            }

            if (string.IsNullOrWhiteSpace(BlobNamePrefix))
            {
                throw new ArgumentNullException(nameof(BlobNamePrefix));
            }

            CopyBlobs.Validate();
        }
    }

    public class CopyBlobsConfiguration
    {
        public bool Enabled { get; set; }
        public string DestContainerSasUri { get; set; }
        public string SourceSasToken { get; set; }

        public void Validate()
        {
            if (!Enabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(DestContainerSasUri))
            {
                throw new ArgumentNullException(nameof(DestContainerSasUri));
            }

            if (string.IsNullOrWhiteSpace(SourceSasToken))
            {
                throw new ArgumentNullException(nameof(SourceSasToken));
            }
        }
    }
}
