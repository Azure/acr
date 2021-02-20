using Microsoft.Azure.Management.ContainerRegistry;
using ContainerRegistryTransfer.Models;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;

namespace ContainerRegistryTransfer.Helpers
{
    public static class AzureHelper
    {
        public static AzureCredentials GetAzureCredentials(AzureEnvironment environment, string tenantId, string miClientId, string spClientId, string spClientSecret)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            if (!string.IsNullOrWhiteSpace(miClientId))
            {
                return new AzureCredentials(
                    new MSILoginInformation(MSIResourceType.VirtualMachine, miClientId),
                    environment,
                    tenantId);
            }
            else if (!string.IsNullOrWhiteSpace(spClientId)
                && !string.IsNullOrWhiteSpace(spClientSecret))
            {
                return new AzureCredentials(
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
                throw new ArgumentNullException("No subscription credential.");
            }
        }

        public static ContainerRegistryManagementClient GetContainerRegistryManagementClient(Options options)
        {
            var credential = GetAzureCredentials(
                options.AzureEnvironment,
                options.TenantId,
                options.MIClientId,
                options.SPClientId,
                options.SPClientSecret);

            var subscriptionId = options.SubscriptionId;

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            var registryClient = new ContainerRegistryManagementClient(credential.WithDefaultSubscription(subscriptionId));
            registryClient.SubscriptionId = subscriptionId;

            return registryClient;
        }

        public static KeyVaultManagementClient GetKeyVaultManagementClient(Options options)
        {
            var credential = GetAzureCredentials(
                options.AzureEnvironment,
                options.TenantId,
                options.MIClientId,
                options.SPClientId,
                options.SPClientSecret);

            var subscriptionId = options.SubscriptionId;

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            var keyVaultClient = new KeyVaultManagementClient(credential.WithDefaultSubscription(subscriptionId));
            keyVaultClient.SubscriptionId = subscriptionId;

            return keyVaultClient;
        }
    }
}
