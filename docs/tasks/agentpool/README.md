---
title: Agent Pools
---

# Running ACR Tasks on Dedicated Agent Pools

## Introduction

ACR Task Agent Pool provides [ACR Task][acr-tasks] execution in dedicated machine pools.

Task Agent Pools provide:

- **VNet Support:** Agent Pools may be assigned to Azure VNets, providing access the resources in the VNet (eg, Container Registry, Key Vault, Storage).
- **Scale As Needed:** Agent pools can be increased as needed, or scaled to zero, billed based on allocation.
- **More Memory and CPU Options:** The current preview provides 3 standard tiers, S1 (2 cpu, 3G mem), S2 (4 cpu, 8G mem), and S3 (8 cpu, 16G mem) and 1 isolated tier, I6 (64 cpu, 216G mem).
- **Agent Pools per Workload:** To serve different configurations, instance agent pools based on scale and tier options to serve different types of workloads.
- **Hybrid Managed Pools:** Task pools are patched and maintained by Azure. Task pools provide a balance between reserved allocation without the need to maintain the individual VMs.

ACR Task Agent Pools are currently previewed in WestUS2, SouthCentralUS, EastUS2, EastUS, CentralUS, USGovArizona, USGovTexas and USGovVirginia.

## Prerequisites

- [Azure CLI][azure-cli] __2.3.1__ or above.
- A [__premium__ container registry][acr-tiers] in the above preview regions.

## Create and Manage an ACR Task Agent Pool

- Set the default registry, simplifying CLI commands

```sh
az configure --defaults acr=[registryName]
```

- Create an agent pool of tier S2 (4 cpu/instance) with 1 instance.

```sh
az acr agentpool create \
    --name myagentpool \
    --tier S2
```

- Scale the agent pool with more instances or scale in to 0.

```sh
az acr agentpool update \
    --name myagentpool \
    --count 2
```

## Create an Agent Pool in a VNet

- Task Agent Pools require access to the following Azure services. The following firewall rules must be added to any existing network security groups or user-defined routes.

| Direction | Protocol | Source         | Source Port | Destination          | Dest Port | Used    |
|-----------|----------|----------------|-------------|----------------------|-----------|---------|
| Outbound  | TCP      | VirtualNetwork | Any         | AzureKeyVault        | 443       | Default |
| Outbound  | TCP      | VirtualNetwork | Any         | Storage              | 443       | Default |
| Outbound  | TCP      | VirtualNetwork | Any         | EventHub             | 443       | Default |
| Outbound  | TCP      | VirtualNetwork | Any         | AzureActiveDirectory | 443       | Default |
| Outbound  | TCP      | VirtualNetwork | Any         | AzureMonitor         | 443       | Default |

[NOTE] If your Tasks require additional resources from public internet, eg, if you run docker build task that needs to pull the base images from DockerHub or restore Nuget package, please add the corresponding rules.

- Create an agent pool in the VNet.

```sh
subnet=$(az network vnet subnet show \
        -g myvnetresourcegroup \
        --vnet-name myvnetname \
        -n mysubnetname \
        --query id -o tsv)

az acr agentpool create \
    --name myagentpool \
    --tier S2 \
    --subnet-id $subnet
```

## Schedule Runs on the Agent Pool

- Schedule a quick run on the agent pool.

```sh
az acr build \
    --agent-pool myagentpool \
    -t myimage:mytag \
    -f Dockerfile \
    https://github.com/Azure-Samples/acr-build-helloworld-node.git
```

- Create a recurring task on the agent pool.

```sh
az acr task create \
    -n mytask \
    --agent-pool myagentpool \
    -t myimage:mytag \
    -f Dcokerfile \
    -c https://github.com/Azure-Samples/acr-build-helloworld-node.git \
    --commit-trigger-enabled false

az acr task run \
    -r mypremiumregistry \
    -n mytask
```

- Query the agent pool queue status (current scheduled runs on the agent pool).

```sh
az acr agentpool show \
    -n myagentpool \
    --queue-count
```

## Preview Limitations

- Task Agent Pools currently support Linux nodes. Windows nodes are not currently supported.
- For each registry, the default total cpu quota of all standard agent pools is 16 and all isolated agent pools is 0. Please [open a support ticket][open-support-ticket] for additional allocation.

[acr-tasks]:           https://aka.ms/acr/tasks
[acr-tiers]:           https://aka.ms/acr/tiers
[azure-cli]:           https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest
[open-support-ticket]: https://aka.ms/acr/support/create-ticket
