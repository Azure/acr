using System;
using System.Collections.Generic;

namespace RegistryArtifactTransfer
{
    public class ImportConfiguration
    {
        public bool Enabled { get; set; }
        public bool Force { get; set; } = true;
        public int MaxConcurrency { get; set; } = 50;
        public int TransferTimeoutInSeconds { get; set; } = 300;
        public SourceRegistryConfiguration SourceRegistry { get; set; }
        public List<string> Repositories { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public string ImportPipelineName { get; set; }
        public List<string> Blobs { get; set; }

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

            if (TransferTimeoutInSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(TransferTimeoutInSeconds), TransferTimeoutInSeconds, "must be larger than 0");
            }

            if (SourceRegistry == null)
            {
                throw new ArgumentNullException(nameof(SourceRegistry));
            }

            SourceRegistry.Validate();

            if (string.IsNullOrWhiteSpace(SourceRegistry.RegistryUri) && (Repositories != null && Repositories.Count > 0))
            {
                throw new ArgumentException($"{nameof(SourceRegistry.RegistryUri)} must be specified to import {nameof(Repositories)}");
            }

            if (string.IsNullOrWhiteSpace(ImportPipelineName) && Blobs != null && Blobs.Count > 0)
            {
                throw new ArgumentNullException(nameof(ImportPipelineName));
            }
        }
    }
}
