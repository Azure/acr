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

## How to get login credentials for a container registry?

Please make sure admin is enabled.

Using `az cli`
```
az acr credential show -n myRegistry
```

Using `Azure Powershell`
```
Invoke-AzureRmResourceAction -Action listCredentials -ResourceType Microsoft.ContainerRegistry/registries -ResourceGroupName myResourceGroup -ResourceName myRegistry
```

## How to get login credentials in an ARM deployment template?

Please make sure admin is enabled.

```
{
    "password": "[listCredentials(resourceId('Microsoft.ContainerRegistry/registries', 'myRegistry'), '2017-03-01').passwords[0].value]"
}
```

To get the second password

```
{
    "password": "[listCredentials(resourceId('Microsoft.ContainerRegistry/registries', 'myRegistry'), '2017-03-01').passwords[1].value]"
}
```

## How to update my registry to use the regenerated storage account access key?

Using `az cli` to update the storage account for your registry
```
az acr update -n myRegistry --storage-account-name myStorageAccount
```

Your can find `myStorageAccount` to your registry by the following command
```
az acr show -n myRegistry --query storageAccount.name
```

## How to log into my registry when running the CLI in a container?

You need to run the CLI container by mounting the Docker socket
```
docker run -it -v /var/run/docker.sock:/var/run/docker.sock azuresdk/azure-cli-python
```

In the container, you can install `docker` by
```
apk add docker
```

Then you can log into your registry by
```
az acr login -n MyRegistry
```
