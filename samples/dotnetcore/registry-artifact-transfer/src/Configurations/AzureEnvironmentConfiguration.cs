using System;

namespace RegistryArtifactTransfer
{
    public class AzureEnvironmentConfiguration
    {
        public string Name { get; set; }
        public string AuthenticationEndpoint { get; set; }
        public string ManagementEndpoint { get; set; }
        public string ResourceManagerEndpoint { get; set; }
        public string GraphEndpoint { get; set; }
        public string KeyVaultSuffix { get; set; }
        public string StorageEndpointSuffix { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ArgumentNullException(nameof(Name));
            }
        }
    }
}