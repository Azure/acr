# ACR Transfer - Sample ARM Templates

This directory contains Azure Resource Manager (ARM) templates for ACR Transfer, a feature for transferring container images and OCI artifacts between Azure Container Registries in disconnected environments.

## Templates

| Directory | Description |
|-----------|-------------|
| [ExportPipelines](./ExportPipelines) | Creates an ExportPipeline resource for exporting artifacts from a container registry to a storage account blob container. |
| [ImportPipelines](./ImportPipelines) | Creates an ImportPipeline resource for importing artifacts from a storage account blob container into a container registry. |
| [PipelineRun/PipelineRun-Export](./PipelineRun/PipelineRun-Export) | Creates a PipelineRun resource to trigger an export pipeline. |
| [PipelineRun/PipelineRun-Import](./PipelineRun/PipelineRun-Import) | Creates a PipelineRun resource to trigger an import pipeline. |

## Documentation

For complete documentation including prerequisites, setup instructions, and usage examples, see:

**[ACR Transfer Documentation](https://aka.ms/acr/transfer)**

The documentation covers:

- What is ACR Transfer and how it works
- Storage access modes (SAS Token vs Managed Identity)
- Prerequisites and setup
- Step-by-step guides for Azure CLI and ARM templates
- Troubleshooting

## Quick Start

These templates require:

- Azure Container Registry Premium tier
- Storage accounts with blob containers
- API version `2025-06-01-preview` or later
- For detailed prerequisites and instructions, refer to the [official documentation](https://aka.ms/acr/transfer)
