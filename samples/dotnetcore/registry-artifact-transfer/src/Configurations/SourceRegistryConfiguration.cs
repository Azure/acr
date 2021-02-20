using System;

namespace RegistryArtifactTransfer
{
    public class SourceRegistryConfiguration
    {
        public string ResourceId { get; set; }
        public string RegistryUri { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public void Validate()
        {
            if (!string.IsNullOrWhiteSpace(ResourceId))
            {
                if (!string.IsNullOrWhiteSpace(RegistryUri) ||
                    !string.IsNullOrWhiteSpace(UserName) ||
                    !string.IsNullOrWhiteSpace(Password))
                {
                    throw new ArgumentException($"{nameof(RegistryUri)}, {nameof(UserName)}, or {nameof(Password)} cannot be used with {nameof(ResourceId)}.");
                }
            }
            else if (string.IsNullOrWhiteSpace(RegistryUri))
            {
                throw new ArgumentException($"Either {nameof(RegistryUri)} or {nameof(ResourceId)} must be specified.");
            }
            else if (string.IsNullOrWhiteSpace(UserName) ^ string.IsNullOrWhiteSpace(Password))
            {
                throw new ArgumentException($"Either {nameof(UserName)} or {nameof(Password)} is missing.");
            }
        }
    }
}
