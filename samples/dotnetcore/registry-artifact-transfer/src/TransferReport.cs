namespace RegistryArtifactTransfer
{
    public class TransferReport
    {
        public TransferResult ImportArtifacts { get; set; } = new TransferResult();
        public TransferResult ImportBlobs { get; set; } = new TransferResult();
        public TransferResult ExportArtifacts { get; set; } = new TransferResult();
        public TransferResult ExportBlobs { get; set; } = new TransferResult();
        public TransferResult CopyBlobs { get; set; } = new TransferResult();
    }
}
