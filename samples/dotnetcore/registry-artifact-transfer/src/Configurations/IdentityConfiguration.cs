using System;

namespace RegistryArtifactTransfer
{
    public class IdentityConfiguration
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                throw new ArgumentNullException(nameof(ClientId));
            }

            if (string.IsNullOrWhiteSpace(ClientSecret))
            {
                throw new ArgumentNullException(nameof(ClientSecret));
            }
        }
    }
}