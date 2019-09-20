# Azure Container Registry - Troubleshooting guide


## I get an error while creating a registry - "Unregistered Subscription specified"
<a name="RegisterSub"></a>
You need to register the subscription using 
Powershell:
```
Register-AzureRmResourceProvider -ProviderNamespace Microsoft.ContainerRegistry 
```
Az CLI:
```
az provider register –n Microsoft.ContainerRegistry 
```

## I'm able to create registry in one region but not in another region
As we add more regions, the service in new region needs to know about your subscription. So please register your subscription again so that ACR service in newer regions will know about your subscription

See [here] (#RegisterSub)

## Azure CLI - I get this error - No resource with type Microsoft.ContainerRegistry/registries can be found with name

<a name="SetCorrectSub"></a>
Please run this command and check if you have set the right subscription
```
az account show
```

Please run this command to set the correct subscription
```
az account set --subscription <correct-subscription>
```

## Azure CLI - Not able to use az cli to query/view my registries

See [this] (#RegisterSub) and [this] (#SetCorrectSub)

## Image exists in my ACR but, docker pull returns "image not found"

Please make sure you login before you pull/push repositories
```
docker login <yourregistry>.azurecr.io
```

## Configuring a custom domain for azure container registry

Azure container registries have a typical login url of the format `*.azurecr.io`. A customer might like to use a custom domain for the registry. Follow [this guide](custom-domain/README.md) to achieve that.

## Moving repositories to a new registry 

To move your repositories to a newly created registry, follow [this guide](move-repositories-to-new-registry/README.md).

## Failed to add a virtual network from a different Azure subscription

If you want to restrict registry access using a virtual network in a different Azure subscription, you will see the following error if the subscription hasn't registered the `Microsoft.ContainerRegistry` resource provider:

```
Failed to save firewall and virtual network settings for container registry 'MyRegistry'. Error: Could not validate network rule - The client '00000000-0000-0000-0000-000000000000' with object id '00000000-0000-0000-0000-000000000000' does not have authorization to perform action 'Microsoft.Network/virtualNetworks/taggedTrafficConsumers/validate/action' over scope '/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/MyRG/providers/Microsoft.Network/virtualNetworks/MyRegistry/taggedTrafficConsumers/Microsoft.ContainerRegistry' or the scope is invalid. If access was recently granted, please refresh your credentials.
```

You need to register the resource provider for Azure Container Registry in that subscription. For example:

Azure CLI

```
az account set --subscription <Name or ID of subscription of virtual network>
az provider register --namespace Microsoft.ContainerRegistry
```

## Check role assignments on a registry

```
az role assignment list --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.ContainerRegistry/registries/<registryName>
```
See [here](https://docs.microsoft.com/cli/azure/role/assignment?view=azure-cli-latest#az-role-assignment-list) for reference
