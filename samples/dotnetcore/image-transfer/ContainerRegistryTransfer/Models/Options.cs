using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;

namespace ContainerRegistryTransfer.Models
{
    public class Options
    {
        public string Environment { get; set; }
        public string TenantId { get; set; }
        public string MIClientId { get; set; }
        public string SPClientId { get; set; }
        public string SPClientSecret { get; set; }
        public string SubscriptionId { get; set; }
        public PipelineConfig ExportPipeline { get; set; }
        public PipelineConfig ImportPipeline { get; set; }
        public PipelineRunConfig ExportPipelineRun { get; set; }

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
}
