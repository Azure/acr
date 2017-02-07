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

# How to use a custom regisry ID

Azure docker registries has a typical ID of the format `*-microsoft.azurecr.io`. A customer might like to have a custom registry ID that associate with its own organization. Follow [this guide](custom-registry-id.md) to achieve that.
