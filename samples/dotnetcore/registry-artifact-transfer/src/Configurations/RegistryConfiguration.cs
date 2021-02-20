using System;

namespace RegistryArtifactTransfer
{
    public class RegistryConfiguration
    {
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroupName { get; set; }
        public string Name { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(TenantId))
            {
                throw new ArgumentNullException(nameof(TenantId));
            }

            if (string.IsNullOrWhiteSpace(SubscriptionId))
            {
                throw new ArgumentNullException(nameof(SubscriptionId));
            }

            if (string.IsNullOrWhiteSpace(ResourceGroupName))
            {
                throw new ArgumentNullException(nameof(ResourceGroupName));
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ArgumentNullException(nameof(Name));
            }
        }
    }
}