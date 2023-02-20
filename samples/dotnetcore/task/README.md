# Getting Started with Container Registry - Manage Container Registry Task - in DotNet ##

* Schedule a new run to build a container image using the local sample project (WeatherService) and push it to the registry.
* Wait for the run completion and download the run log.

## Running this sample ##

* Create a container registry
* Create a service principal and assign it the contributor role of the registry
* Build ManageTask.csproj (DotNet SDK 6.0 required)

```sh
dotnet build ManageTask/ManageTask.csproj

TenantId=<ServicePrincipalTeanantId> \
SPClientId=<ServicePrincipalClientId> \
SPClientSecret=<ServicePrincipalPassword> \
SubscriptionId=<RegistrySubscriptionId> \
ResourceGroupName=<RegistryResourceGroupName> \
RegistryName=<RegistryName> \
dotnet ManageTask/bin/Debug/net6.0/ManageTask.dll
```

## More information ##

[https://github.com/Azure/azure-sdk-for-net](https://github.com/Azure/azure-sdk-for-net)

If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212).
