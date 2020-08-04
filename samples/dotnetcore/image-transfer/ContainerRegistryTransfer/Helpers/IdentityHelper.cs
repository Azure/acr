using Microsoft.Azure.Management.ContainerRegistry.Models;
using System.Collections.Generic;
using System.Linq;

namespace ContainerRegistryTransfer.Helpers
{
    public static class IdentityHelper
    {
        public static IdentityProperties GetManagedIdentity(string userAssignedIdentity)
        {
            if (!string.IsNullOrEmpty(userAssignedIdentity))
            {
                return new IdentityProperties
                {
                    Type = ResourceIdentityType.UserAssigned,
                    UserAssignedIdentities = new Dictionary<string, UserIdentityProperties>
                    {
                        { userAssignedIdentity, new UserIdentityProperties() }
                    }
                };
            }
            else
            {
                return new IdentityProperties
                {
                    Type = ResourceIdentityType.SystemAssigned
                };
            }
        }

        public static string GetManagedIdentityPrincipalId(IdentityProperties identity)
        {
            return identity.PrincipalId ?? identity.UserAssignedIdentities.First().Value.PrincipalId;
        }
    }
}
