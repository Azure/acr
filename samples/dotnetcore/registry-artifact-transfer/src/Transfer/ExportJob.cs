using System.Collections.Generic;

namespace RegistryArtifactTransfer
{
    public class ExportJob
    {
        public string ExportPipelineName { get; set; }
        public string PipelineRunName { get; set; }
        public string ExportBlobName { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public TransferJobStatus Status { get; set; }
    }
}