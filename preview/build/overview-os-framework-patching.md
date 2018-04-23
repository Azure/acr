# Addition of Lifecycle Management – OS & Framework Patching
As containers provide new levels of virtualization, enabling application and developer dependencies to be isolated from the infrastructure and operational requirements, we must address how the application virtualization is patched. Azure Container Registry provides a native container build service, enabling inner-loop development in the cloud and automated builds from git commits to base image updates. With base image update triggers, customers can automate their OS & Framework patching needs, maintaining secure environments, while adhering to the principals of immutable containers. ACR Build will dynamically discover base image dependencies. For preview, base images are limited to the same registry. 

ACR Build was designed as a container lifecycle primitive, that will extend and/or integrate into your CI/CD solution.
Using `az login` with [a service principal
](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli?view=azure-cli-latest#log-in-with-a-service-principal), your CI/CD solution may call `az acr build ...` commands.

# Inner-loop, extended to the cloud

The beginning of lifecycle management starts before developers check-in their first lines of code. ACR Build enables an integrated local, inner-loop development experience, offloading builds to Azure. Developers can verify their automated build definitions, prior to checking in their code. Using the familiar docker build format, `az acr build` will take a local context, send it to the acr build service, optionally pushing to its registry upon completion. ACR Build will follow your geo-replicated registries, enabling dispersed development teams to leverage the closest replicated datacenter. For preview, ACR build service will support East US and West Europe. 

