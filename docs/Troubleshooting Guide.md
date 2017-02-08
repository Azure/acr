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

# How to use a custom domain for azure container registry

Azure docker registries has a typical login url of the format `*.azurecr.io`. A customer might like to have a custom domain for the registry. Follow [this guide](custom-domain.md) to achieve that.
