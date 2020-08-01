using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Collections.Generic;
using System.Text;

namespace ContainerRegistryTransfer
{
    public class Options
    {
        public string Environment { get; set; }
        public string TenantId { get; set; }
        public string MIClientId { get; set; }
        public string SPClientId { get; set; }
        public string SPClientSecret { get; set; }
        public string SubscriptionId { get; set; }
        public PipelineProperties ExportPipeline { get; set; }
        public PipelineProperties ImportPipeline { get; set; }
        public PipelineRunProperties ExportPipelineRun { get; set; }

        public AzureEnvironment AzureEnvironment
        {
            get
            {
                return string.IsNullOrWhiteSpace(Environment)
                    ? AzureEnvironment.AzureGlobalCloud
                    : AzureEnvironment.FromName(Environment);
            }
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(TenantId))
            {
                throw new ArgumentNullException(nameof(TenantId));
            }

            if (string.IsNullOrWhiteSpace(MIClientId)
                && (string.IsNullOrWhiteSpace(SPClientId) || string.IsNullOrWhiteSpace(SPClientSecret)))
            {
                throw new ArgumentNullException($"Missing {nameof(MIClientId)} or {nameof(SPClientId)}/{nameof(SPClientSecret)}");
            }

            if (string.IsNullOrWhiteSpace(SubscriptionId))
            {
                throw new ArgumentNullException(nameof(SubscriptionId));
            }

            ExportPipeline.Validate();
            ImportPipeline.Validate();
        }
    }

    public class PipelineProperties
    {
        public string ResourceGroupName { get; set; }

        public string RegistryName { get; set; }

        public string PipelineName { get; set; }

        public string KeyVaultUri { get; set; }

        public string ContainerUri { get; set; }

        public string UserAssignedIdentity { get; set; }

        public List<string> Options { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ResourceGroupName))
            {
                throw new ArgumentNullException(nameof(ResourceGroupName));
            }

            if (string.IsNullOrWhiteSpace(RegistryName))
            {
                throw new ArgumentNullException(nameof(RegistryName));
            }

            if (string.IsNullOrWhiteSpace(KeyVaultUri))
            {
                throw new ArgumentNullException(nameof(KeyVaultUri));
            }

            if (string.IsNullOrWhiteSpace(ContainerUri))
            {
                throw new ArgumentNullException(nameof(ContainerUri));
            }

            if (string.IsNullOrWhiteSpace(PipelineName))
            {
                throw new ArgumentNullException(nameof(PipelineName));
            }
        }
    }

    public class PipelineRunProperties
    {
        public string PipelineRunName { get; set; }

        public string TargetName { get; set; }

        public List<string> Artifacts { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(PipelineRunName))
            {
                throw new ArgumentNullException(nameof(PipelineRunName));
            }

            if (string.IsNullOrWhiteSpace(TargetName))
            {
                throw new ArgumentNullException(nameof(TargetName));
            }

            if (Artifacts == null || Artifacts.Count == 0)
            {
                throw new ArgumentNullException(nameof(Artifacts));
            }
        }
    }
}
