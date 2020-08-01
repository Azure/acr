using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace ContainerRegistryTransfer.Helpers
{
    public class KeyVaultHelper
    {
        public static void AddKeyVaultAccessPolicy(KeyVaultManagementClient keyVaultClient, string tenantId, string subscriptionId, string resourceGroupName, string vaultName, string identityPrincipalId)
        {
            var vault = keyVaultClient.Vaults.Get(resourceGroupName, vaultName);

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

                    keyVaultClient.Vaults.UpdateAccessPolicy(
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
                        });
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
