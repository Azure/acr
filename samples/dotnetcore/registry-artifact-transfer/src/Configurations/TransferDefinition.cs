namespace RegistryArtifactTransfer
{
    public class TransferDefinition
    {
        public AzureEnvironmentConfiguration AzureEnvironment { get; set; }
        public IdentityConfiguration Identity { get; set; }
        public RegistryConfiguration Registry { get; set; }
        public ImportConfiguration Import { get; set; }
        public ExportConfiguration Export { get; set; }

        public void Validate()
        {
            AzureEnvironment.Validate();
            Identity.Validate();
            Registry.Validate();
            Import.Validate();
            Export.Validate();
        }
    }
}
