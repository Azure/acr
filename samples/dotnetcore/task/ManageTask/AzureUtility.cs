using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using System;

namespace ManageTask
{
    internal class AzureUtility
    {
        private readonly TokenCredential credential;

        public ArmClient ArmClient { get; private set; }

        public AzureUtility(Uri azureAuthorityHosts, string subscriptionId, string miClientId, string tenantId, string spClientId, string spClientSecret)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (!string.IsNullOrWhiteSpace(miClientId))
            {
                credential = new ManagedIdentityCredential(
                    clientId: miClientId,
                    options: new TokenCredentialOptions
                    {
                        AuthorityHost = azureAuthorityHosts
                    });
            }
            else if (!string.IsNullOrWhiteSpace(spClientId)
                && !string.IsNullOrWhiteSpace(spClientSecret)
                && !string.IsNullOrWhiteSpace(tenantId))
            {
                credential = new ClientSecretCredential(
                    tenantId: tenantId,
                    clientId: spClientId,
                    clientSecret: spClientSecret,
                    options: new TokenCredentialOptions
                    {
                        AuthorityHost = azureAuthorityHosts
                    });
            }
            else
            {
                //credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
                credential = new DeviceCodeCredential(
                    options: new DeviceCodeCredentialOptions
                    { 
                        AuthorityHost = azureAuthorityHosts
                    });
            }

            ArmClient = new ArmClient(credential, subscriptionId);
        }
    }
}
