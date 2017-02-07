# Azure Container Registry - Frequently Asked Questions

## Can I create Azure Container Registry using ARM Template?
Yes. Here is the template that you can use to create a registry - https://github.com/Azure/azure-cli/blob/master/src/command_modules/azure-cli-acr/azure/cli/command_modules/acr/template.json

## Is there security vulnerability scanning for images in ACR?

Yes. Please check the following links

Twistlock - https://www.twistlock.com/2016/11/07/twistlock-supports-azure-container-registry/

Aqua - http://blog.aquasec.com/image-vulnerability-scanning-in-azure-container-registry


## How to configure Kubernetes with Azure Container Registry?
http://kubernetes.io/docs/user-guide/images/#using-azure-container-registry-acr


## How to access Docker Registry HTTP API V2?
ACR supports Docker Registry HTTP API V2. The APIs can be accessed at
https://\<your registry login server\>/v2/

## Is Azure Premium Storage account supported?
Azure Premium Storage account is not supported.

## How to use Powershell to manage Azure Container Registry?

See [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/powershell-azure-resource-manager) for an introduction to manage Azure resources with PowerShell.

To set the correct subscription
```
Select-AzureRmSubscription -SubscriptionName mySubscription
```

To show a container registry
```
Get-AzureRmResource -ResourceType Microsoft.ContainerRegistry/registries -ResourceGroupName myResourceGroup -ResourceName myRegistry
```

To show admin login credentials
```
Invoke-AzureRmResourceAction -Action getCredentials -ResourceType Microsoft.ContainerRegistry/registries -ResourceGroupName myResourceGroup -ResourceName myRegistry
```
