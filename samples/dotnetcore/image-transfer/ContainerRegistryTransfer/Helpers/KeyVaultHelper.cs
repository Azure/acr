using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using System;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;

namespace ContainerRegistryTransfer.Helpers
{
    public static class KeyVaultHelper
    {
        public static async Task AddKeyVaultAccessPolicyAsync(KeyVaultManagementClient keyVaultClient, string tenantId, string subscriptionId, string resourceGroupName, string vaultName, string identityPrincipalId)
        {
            var vault = await keyVaultClient.Vaults.GetAsync(resourceGroupName, vaultName).ConfigureAwait(false);

            if (vault != null)
            {
                var accessPolicy = new AccessPolicyEntry
                {
                    TenantId = new System.Guid(tenantId),
                    ObjectId = identityPrincipalId,
                    Permissions = new Permissions
                    {
                        Secrets = new List<string>
                        {
                            { "get" }
                        }
                    }
                };

                if (vault.Properties.AccessPolicies.Contains(accessPolicy))
                {
                    Console.WriteLine($"The vault '{vaultName}' already contains this access policy for principalId '{identityPrincipalId}'. Skip.");
                }
                else
                {
                    Console.WriteLine($"Adding access policy for principalId '{identityPrincipalId} to the vault '{vaultName}'.");

                    await keyVaultClient.Vaults.UpdateAccessPolicyAsync(
                        resourceGroupName,
                        vaultName,
                        AccessPolicyUpdateKind.Add,
                        new VaultAccessPolicyParameters
                        {
                            Properties = new VaultAccessPolicyProperties
                            {
                                AccessPolicies = new List<AccessPolicyEntry>()
                                {
                                    accessPolicy
                                }
                            }
                        }).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentException($"Could not find key vault '{vaultName}'. Please ensure the vault exists in the current resource group {resourceGroupName}.");
            }
        }

        public static string GetKVNameFromUri(string keyVaultUri)
        {
            var vaultUri = new Uri(keyVaultUri);
            return vaultUri?.Host?.Split('.')[0];
        }
    }
}
