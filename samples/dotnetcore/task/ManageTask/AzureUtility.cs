using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;

namespace ManageTask
{
    internal class AzureUtility
    {
        private readonly AzureCredentials credential;

        public ContainerRegistryManagementClient RegistryClient { get; private set; }

        public AzureUtility(AzureEnvironment environment, string tenantId, string subscriptionId, string miClientId, string spClientId, string spClientSecret)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (!string.IsNullOrWhiteSpace(miClientId))
            {
                credential = new AzureCredentials(
                    new MSILoginInformation(MSIResourceType.VirtualMachine, miClientId),
                    environment,
                    tenantId);
            }
            else if (!string.IsNullOrWhiteSpace(spClientId)
                && !string.IsNullOrWhiteSpace(spClientSecret))
            {
                credential = new AzureCredentials(
                    new ServicePrincipalLoginInformation
                    {
                        ClientId = spClientId,
                        ClientSecret = spClientSecret
                    },
                    tenantId,
                    environment);
            }
            else
            {
                throw new ArgumentNullException("No subscription credential");
            }

            RegistryClient = new ContainerRegistryManagementClient(credential.WithDefaultSubscription(subscriptionId));
            RegistryClient.SubscriptionId = subscriptionId;
        }
    }
}
