using System;
using System.Collections.Generic;

namespace ContainerRegistryTransfer.Models
{
    public class PipelineRunConfig
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
