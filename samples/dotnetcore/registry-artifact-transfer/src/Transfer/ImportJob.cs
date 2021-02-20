using System;
using System.Collections.Generic;

namespace RegistryArtifactTransfer
{
    public class ImportJob
    {
        public ImportSourceType SourceType { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public string ImportPipelineName { get; set; }
        public string PipelineRunName { get; set; }
        public string ImportBlobName { get; set; }
        public TransferJobStatus Status { get; set; } = TransferJobStatus.Pending;
    }

    public enum ImportSourceType
    {
        AzureStorageBlob,
        RegistryImage
    }
}