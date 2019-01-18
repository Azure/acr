---
title: Deploy an Azure container registry into a virtual network
description: Deploy an Azure container registry to an Azure virtual network to restrict access to networked resources.
services: container-registry
author: dlepow

ms.service: container-registry
ms.topic: article
ms.date: 01/08/2019
ms.author: danlep
---

# Deploy an Azure container registry in an Azure virtual network

[Azure Virtual Network](https://docs.microsoft.com/azure/virtual-network/virtual-networks-overview) provides secure, private networking including filtering, routing, and peering for your Azure and on-premises resources. By deploying your private Azure container registry into an Azure virtual network, you can ensure that only resources in the virtual network are able to access the registry.

For example, use a virtual network to permit Azure resources such as an [Azure Kubernetes Service](https://docs.microsoft.com/azure/aks/intro-kubernetes) cluster or Azure [virtual machine](https://docs.microsoft.com/azure/virtual-machines/linux/overview) configured as a Docker host to securely access the container registry without crossing a network boundary.

> [!IMPORTANT]
> This feature is currently in preview, and some [limitations apply](#preview-limitations). Previews are made available to you on the condition that you agree to the [supplemental terms of use][terms-of-use]. Some aspects of this feature may change prior to general availability (GA).
>

This article shows you how to access a container registry deployed in a virtual network from a virtual machine in the same network.

This article requires that you are running the Azure CLI version 2.0.27 or later. If you need to install or upgrade, see [Install Azure CLI][azure-cli].

For this example, you must also have Docker installed locally. Docker provides packages that easily configure Docker on any [macOS][docker-mac], [Windows][docker-windows], or [Linux][docker-linux] system.

## Preview limitations

The following limitations currently apply to the preview:

* Make sure that your request for preview access is approved and your Azure subscription is whitelisted.

* Only an Azure virtual machine or Azure AKS cluster can be used as a host to access a container registry in a virtual network. Other Azure services including Azure Container Instances aren't currently supported.
* You can only use the Azure portal to manage default network access rules for a container registry.
* IP network rules for access from specific public IP address ranges are not currently supported.

## Virtual network configuration overview

The following general steps configure a container registry to allow access only from a virtual network:

1. Configure the virtual network with a [service endpoint](https://docs.microsoft.com/azure/virtual-network/virtual-network-service-endpoints-overview) for the Azure Container Registry service. 

   A service endpoint represents a range of IP addresses in Azure regions that make resources for an Azure service available only from a virtual network. This endpoint gives traffic an optimal route to the service over the Azure backbone network. The identities of the virtual network and the subnet are also transmitted with each request.

1. Configure network rules for a specific container registry that allow requests to be received from specific subnets in each virtual network. 


   Clients granted access via these network rules must continue to [authenticate to the container registry](https://docs.microsoft.com/azure/container-registry/container-registry-authentication) and be authorized to access the data.

### Virtual network deployment limitations

* Only a **Premium** container registry can be deployed to a virtual network. For information about registry service tiers, see [Azure Container Registry SKUs](https://docs.microsoft.com/azure/container-registry/container-registry-skus).

* The container registry and virtual network must be in the same Azure subscription and region.

* Each registry supports up to 100 virtual network rules.

## Create a Docker-enabled virtual machine

For this article, you will use a Docker-enabled Ubuntu virtual machine in a virtual network to access an Azure container registry. If you already have an Azure virtual machine, skip this first step to create the virtual machine.

First, create a resource group to contain the virtual machine and virtual network. Create a resource group with [az group create](https://docs.microsoft.com/cli/azure/group#az_group_create). The following example creates a resource group named *myResourceGroup* in the *westcentralus* location:

```azurecli
az group create --name myResourceGroup --location westcentralus
```

Now deploy a default Ubuntu Azure virtual machine with [az vm create][az-vm-create]. The following example creates a VM named *myDockerVM*:

```azurecli
az vm create \
    --resource-group myResourceGroup \
    --name myDockerVM \
    --image UbuntuLTS \
    --admin-username azureuser \
    --generate-ssh-keys
```

It takes a few minutes for the VM to be created. When the command completes, take note of the `publicIpAddress` displayed by the Azure CLI. Use this address to make SSH connections to the VM.

### Install Docker on the VM

After the VM is running, make an SSH connection to the VM. Replace *publicIpAddress* with the public IP address of your VM.

```bash
ssh azureuser@publicIpAddress
```
Run the following command to install Docker on the VM:

```bash
sudo apt install docker.io -y
```

After installation, run the following command to verify that Docker is running properly on the VM:

```bash
sudo docker run -it hello-world
```

Output:

```
Hello from Docker!
This message shows that your installation appears to be working correctly.
[...]
```

Exit the SSH connection.

## Add a service endpoint to the virtual network

When you create a VM, Azure by default creates a virtual network in the same resource group. The name of the virtual network is based on the name of the virtual machine. For example, if you name your virtual machine *myDockerVM*, the virtual network is named *myDockerVMVNET* by default, with a subnet named *myDockerVMSubnet*. Verify this in the Azure portal or by using the [az network vnet list][az-network-vnet-list] command:

```azurecli
az network vnet list --resource-group myResourceGroup --query "[].{Name: name, Subnet: subnets[0].name}"
```

Output:

```console
[
  {
    "Name": "myDockerVMVNET",
    "Subnet": "myDockerVMSubnet"
  }
]
```

Use the [az network vnet subnet update][az-network-vnet-subnet-update] command to add a **Microsoft.ContainerRegistry** service endpoint to your virtual network and subnet. Substitute the names of your virtual network and subnet in the following command:

```azurecli
az network vnet subnet update \
  --name myDockerVMSubnet \
  --vnet-name myDockerVMVNET \
  --resource-group myResourceGroup \
  --service-endpoints Microsoft.ContainerRegistry
```

## Create and configure a container registry 

Create an Azure container registry instance using the [az acr create][az-acr-create] command. The registry name must be unique within Azure, and contain 5-50 alphanumeric characters. In the following example, *myContainerRegistryxxx* is used. Update this to a unique value.

```azurecli
az acr create --resource-group myResourceGroup --name myContainerRegistryxxx --sku Premium
```

### Log in to the registry

Before pushing and pulling container images, you must log in to the registry instance. To do so, use the [az acr login][az-acr-login] command.

```azurecli
az acr login --name myContainerRegistryxxx
```

The command returns a `Login Succeeded` message once completed.

### Push a sample image to the registry

To push an image to an Azure container registry, you must first have an image. If you don't yet have any local container images, run the following local Docker command to pull an existing image from Docker Hub.

```bash
docker pull microsoft/aci-helloworld
```

Before you can push an image to your registry, you must tag it with the fully qualified name of your ACR login server, in this example *mycontainerregistryxxx*.azurecr.io. 

Tag the image using the [docker tag][docker-tag] command. 

```bash
docker tag microsoft/aci-helloworld mycontainerregistryxxx.azurecr.io/aci-helloworld:v1
```

Finally, use [docker push][docker-push] to push the image to the registry. 

```bash
docker push mycontainerregistryxxx/aci-helloworld:v1
```

### Configure registry authentication

For this example, you configure registry access using [service principal authentication](https://docs.microsoft.com/azure/container-registry/container-registry-auth-service-principal). 

To create a service principal, use the [service-principal-create.sh](https://github.com/Azure-Samples/azure-cli-samples/blob/master/container-registry/service-principal-create/service-principal-create.sh) script on GitHub. For this example, assign the `acrpull` role to the service principal, to allow pulling images. 

The script returns values for the service principal application ID and password. Take note of these values for a later step.

## Configure network access for container registry

By default, an Azure container registry accepts connections from hosts on any network. To limit access to a selected network, you must change the default action. In preview, you can manage default network access rules for a container registry only through the Azure portal.

1. Sign in to the preview Azure portal at [https://aka.ms/acr/portal/vnet](https://aka.ms/acr/portal/vnet).
1. In the portal, navigate to your registry and select **Firewall and virtual networks**.
1. To deny access by default, choose to allow access from **Selected networks**. 
1. Select **Add existing virtual network**, and select the virtual network and subnet you created previously. Select **Add**. 
1. Select **Save**.

  ![Configure virtual network for container registry](./media/container-registry-vnet/acr-vnet-portal.png)

## Verify access to the registry

Now verify that the VM in the virtual network can access the container registry. Make an SSH connection to your VM, and run the following command to login to your registry using your service principal credentials:

```bash
sudo docker login --username <SP_APP_ID> --password <SP_PASSWD> mycontainerregistryxxx.azurecr.io
```

You can pull the sample image in your registry to your VM. Substitute the image and tag that you pushed in an earlier step.

```bash
sudo docker pull mycontainerregistryxxx.azurecr.io/aci-helloworld:v1
``` 

Docker successfully pulls the image to the VM.

Although you are able to access the private container registry from the VM over the virtual network, the registry can't be accessed from other networks. For example, you can't login to the registry over the internet from a different login host (such as your local computer) using the `docker login` command. 

```bash
sudo docker login --username <SP_APP_ID> --password <SP_PASSWD> mycontainerregistryxxx.azurecr.io
```

Output indicates that the registry can't be accessed in this case:

```Console
Error response from daemon: Get https://mycontainerregistryxxx.azurecr.io/v2/ failed with status: 404 Not Found
```

This example demonstrates that only a Docker host in the virtual network is able to access the registry.

## Clean up resources

If you followed the steps in this article to create the Azure resources in the same resource group, you can delete the resources when done by using the [az group delete](/cli/azure/group#az_group_delete) command:

```azurecli
az group delete --name myResourceGroup
```

## Next steps

Several virtual network resources and features were discussed in this article, though briefly. The Azure Virtual Network documentation covers these topics extensively:

* [Virtual network](https://docs.microsoft.com/azure/virtual-network/manage-virtual-network)
* [Subnet](https://docs.microsoft.com/azure/virtual-network/virtual-network-manage-subnet)
* [Service endpoints](https://docs.microsoft.com/azure/virtual-network/virtual-network-service-endpoints-overview)

<!-- IMAGES -->

<!-- LINKS - External -->
[aci-helloworld]: https://hub.docker.com/r/microsoft/aci-helloworld/
[terms-of-use]: https://azure.microsoft.com/support/legal/preview-supplemental-terms/
[kubectl]: https://kubernetes.io/docs/user-guide/kubectl/
[docker-linux]: https://docs.docker.com/engine/installation/#supported-platforms
[docker-login]: https://docs.docker.com/engine/reference/commandline/login/
[docker-mac]: https://docs.docker.com/docker-for-mac/
[docker-push]: https://docs.docker.com/engine/reference/commandline/push/
[docker-tag]: https://docs.docker.com/engine/reference/commandline/tag/
[docker-windows]: https://docs.docker.com/docker-for-windows/

<!-- LINKS - Internal -->
[azure-cli]: https://docs.microsoft.com/cli/azure/install-azure-cli
[az-acr-create]: https://docs.microsoft.com/cli/azure/acr#az-acr-create
[az-acr-show]: https://docs.microsoft.com/cli/azure/acr#az-acr-show
[az-acr-repository-show]: https://docs.microsoft.com/cli/azure/acr/repository#az-acr-repository-show
[az-acr-repository-list]: https://docs.microsoft.com/cli/azure/acr/repository#az-acr-repository-list
[az-acr-login]: https://docs.microsoft.com/cli/azure/acr#az-acr-login
[az-acr-run]: https://docs.microsoft.com/cli/azure/acr#az-acr-run
[az-ad-sp-create-for-rbac]: https://docs.microsoft.com/cli/azure/ad/sp#az-ad-sp-create-for-rbac
[az-role-assignment-create]: https://docs.microsoft.com/cli/azure/role/assignment#az-role-assignment-create
[az-vm-create]: https://docs.microsoft.com/cli/azure/vm#az-vm-create
[az-network-vnet-subnet-show]: https://docs.microsoft.com/cli/azure/network/vnet/subnet/#az-network-vnet-subnet-show
[az-network-vnet-subnet-update]: https://docs.microsoft.com/cli/azure/network/vnet/subnet/#az-network-vnet-subnet-update
[az-network-vnet-list]: https://docs.microsoft.com/cli/azure/network/vnet/#az-network-vnet-list