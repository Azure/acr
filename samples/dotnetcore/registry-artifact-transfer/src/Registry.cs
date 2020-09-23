using System;
using static RegistryArtifactTransfer.ResourceId;

namespace RegistryArtifactTransfer
{
    public class Registry
    {
        public ResourceId ResourceId { get; }
        public string TenantId { get; }
        public string LoginServer { get; }
        public string UserName { get; }
        public string Password { get; }

        public Registry(
            string tenantId,
            string subscriptionId,
            string resourceGroupName,
            string registryName)
        {
            ResourceId = new ResourceId(subscriptionId, resourceGroupName, registryName, RegistriesARMResourceType);
            TenantId = tenantId;
        }

        public Registry(
            string loginServer,
            string userName,
            string password)
        {
            LoginServer = loginServer;
            UserName = userName;
            Password = password;
        }

        public void Validate()
        {
            if (ResourceId == null && string.IsNullOrEmpty(LoginServer))
            {
                throw new ArgumentException($"Neither {nameof(ResourceId)} nor {nameof(LoginServer)} is specified.");
            }

            if (string.IsNullOrEmpty(UserName) ^ string.IsNullOrEmpty(Password))
            {
                throw new ArgumentException($"{nameof(UserName)} and {nameof(Password)} should either be both specified or undeclared.");
            }
        }
    }
}
