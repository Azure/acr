using System;

namespace RegistryArtifactTransfer
{
    public class ResourceId
    {
        public const string ContainerRegistryProviderNamespace = "Microsoft.ContainerRegistry";
        public const string RegistriesARMResourceType = ContainerRegistryProviderNamespace + "/registries";
        public const string ExportPipelineResourceType = "exportPipelines";
        public const string ImportPipelineResourceType = "importPipelines";
        public const string PipelineRunResourceType = "pipelineRuns";

        public string SubscriptionId { get; set; }

        public string ResourceGroupName { get; set; }

        public string ResourceName { get; set; }

        public string ArmResourceType { get; set; }

        // Current only support single-level nested resources.
        public string ChildResourceName { get; set; }

        public string ChildResourceType { get; set; }

        public ResourceId()
        {
        }

        public ResourceId(
            string subscriptionId,
            string resourceGroupName,
            string resourceName,
            string armResourceType)
        {
            this.SubscriptionId = subscriptionId ?? throw new ArgumentNullException(nameof(subscriptionId));
            this.ResourceGroupName = resourceGroupName ?? throw new ArgumentNullException(nameof(resourceGroupName));
            this.ResourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
            this.ArmResourceType = armResourceType ?? throw new ArgumentNullException(nameof(armResourceType));
            this.ChildResourceName = null;
            this.ChildResourceType = null;
        }

        public ResourceId(
            string subscriptionId,
            string resourceGroupName,
            string resourceName,
            string armResourceType,
            string childResourceType,
            string childResourceName) :
            this(
                subscriptionId,
                resourceGroupName,
                resourceName,
                armResourceType)
        {
            this.ChildResourceName = childResourceName ?? throw new ArgumentNullException(nameof(childResourceName));
            this.ChildResourceType = childResourceType ?? throw new ArgumentNullException(nameof(childResourceType));
        }

        public static bool TryParse(string value, out ResourceId resourceId)
        {
            try
            {
                resourceId = Parse(value);
                return true;
            }
            catch (FormatException)
            {
                resourceId = default(ResourceId);
                return false;
            }
        }

        public static ResourceId Parse(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var components = value.Split('/');

            // The accepted string values are:
            // "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}"
            // "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{provideName}/{resourceType}/{resourceName}" 
            // "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{provideName}/{resourceType}/{resourceName}/{childResourceType}/{childResourceName}" 

            if ((components.Length != 5 && components.Length != 9 && components.Length != 11) ||
                !string.IsNullOrEmpty(components[0]) ||
                !string.Equals(components[1], "subscriptions", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(components[3], "resourceGroups", StringComparison.OrdinalIgnoreCase) ||
                (components.Length > 5 &&
                !string.Equals(components[5], "providers", StringComparison.OrdinalIgnoreCase)))
            {
                throw new FormatException("Failed to parse a resource id from the input string: \"" + value + "\"");
            }

            var resourceId = new ResourceId()
            {
                SubscriptionId = components[2],
                ResourceGroupName = components[4]
            };

            if (components.Length > 5)
            {
                resourceId.ArmResourceType = string.Join("/", components[6], components[7]);
                resourceId.ResourceName = components[8];
            }

            if (components.Length > 9)
            {
                resourceId.ChildResourceType = components[9];
                resourceId.ChildResourceName = components[10];
            }

            return resourceId;
        }

        public override string ToString()
        {
            var resourceIdString = GetParentResourceId();

            if (!string.IsNullOrEmpty(ChildResourceType))
            {
                resourceIdString = string.Join("/", resourceIdString, ChildResourceType, ChildResourceName);
            }

            return resourceIdString;
        }

        public string GetParentResourceId()
        {
            var resourceIdString = string.Join("/", string.Empty, "subscriptions", SubscriptionId, "resourceGroups", ResourceGroupName);

            if (!string.IsNullOrEmpty(ArmResourceType))
            {
                resourceIdString = string.Join("/", resourceIdString, "providers", ArmResourceType, ResourceName);
            }

            return resourceIdString;
        }
    }
}
