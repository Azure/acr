using System;
using System.Collections.Generic;

namespace ContainerRegistryTransfer.Models
{
    public class PipelineConfig
    {
        public string ResourceGroupName { get; set; }

        public string RegistryName { get; set; }

        public string PipelineName { get; set; }

        public string KeyVaultUri { get; set; }

        public string ContainerUri { get; set; }

        // Resource ID of the user assigned managed identity. If this property is ommitted, 
        // a system-assigned identity will be provisioned for the pipeline.
        public string UserAssignedIdentity { get; set; }

        public List<string> Options { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ResourceGroupName))
            {
                throw new ArgumentNullException(nameof(ResourceGroupName));
            }

            if (string.IsNullOrWhiteSpace(RegistryName))
            {
                throw new ArgumentNullException(nameof(RegistryName));
            }

            if (string.IsNullOrWhiteSpace(KeyVaultUri))
            {
                throw new ArgumentNullException(nameof(KeyVaultUri));
            }

            if (string.IsNullOrWhiteSpace(ContainerUri))
            {
                throw new ArgumentNullException(nameof(ContainerUri));
            }

            if (string.IsNullOrWhiteSpace(PipelineName))
            {
                throw new ArgumentNullException(nameof(PipelineName));
            }
        }
    }
}
