---
title: Quickstart - Build container images in Azure with Azure Container Registry Build
description: Quickly learn how to build Docker container images in Azure with Azure Container Registry Build (ACR Build), then deploy it to Azure Container Instances.
services: container-registry
author: mmacy
manager: timlt

ms.service: container-registry
ms.topic: article
ms.date: 04/03/2018
ms.author: marsma
---

# Use Azure Container Registry Build for quick image build and deployment

Azure Container Registry's **ACR Build** feature extends your development "inner loop" to the cloud, building your container images in Azure when you commit your code. It also enables you to work locally, building container images in the cloud without requiring a local Docker Engine installation. ACR Build can speed deployment and validation of containers since you don't need a separate `docker push` operation to prop your images to your container registry.

In this quickstart, you build a container image from source code in Azure with ACR Build, then test it with a deployment to Azure Container Instances.

> IMPORTANT: ACR Build is in currently in preview, and is supported **only** by Azure container registries in the **EastUS** region. Previews are made available to you on the condition that you agree to the [supplemental terms of use][terms-of-use]. Some aspects of this feature may change prior to general availability (GA).

## Get ACR Build

* **Access**: While ACR Build is in preview, you must first request access at https://aka.ms/acr/preview/signup

* **Installation**: Next, [install the ACR Build preview](../install.md), which enables the `az acr build` command in the Azure CLI.

## Build locally with Docker Engine

To compare the two image build methods--local build and ACR Build--first clone the following Git repo and build it locally. In the next section you use ACR Build to build the image in Azure, then deploy a container from the image to Azure Container Instances (ACI).

1. Clone the sample repo from GitHub:

    ```sh
    git clone https://github.com/Azure-Samples/aci-helloworld
    ```

1. Enter the directory containing the cloned source code:

    ```sh
    cd aci-helloworld
    ```

1. Build and run locally. This step is actually *optional*, and isn't needed when you use ACR Build to build your images. In this article, it's used only as a comparison with `az acr build`. If you don't have Docker running locally, you can skip this step and move directly to [Build in Azure with ACR Build](#build-in-azure-with-acr-build).

    ```sh
    # Build image locally
    docker build -t helloacrbuild:v1 .
    # Run the image
    docker run -d -p 8088:80 helloacrbuild:v1
    ```

    View the locally running application by navigating to http://localhost:8088 in your browser.

## Build in Azure with ACR Build

Now that you've pulled the source code down to your machine, follow these steps to create a container registry and build the container image with **ACR Build**.

The example below creates an Azure container registry named **mycontainerregistry**. Because this registry name might already be taken, replace **mycontainerregistry** with a unique name for your registry. The registry name must be unique within Azure and contain 5-50 alphanumeric characters. You're also welcome to change the resource group name defined in `RES_GROUP`.

> NOTE: ACR Build is currently supported only by registries in **EastUS**. Do not change the location of the resource group.

1. Create a registry and log in to it with your Azure ID:

    ```sh
    ACR_NAME=mycontainerregistry # Registry name - must be *unique* within Azure
    RES_GROUP=$ACR_NAME # Resource Group name

    az group create -g $RES_GROUP -l eastus
    az acr create -g $RES_GROUP -n $ACR_NAME --sku Standard
    az acr login -n $ACR_NAME
	```

1. Build the image with **ACR Build**:

    ```sh
    az acr build -t helloacrbuild:v1 --context . --registry $ACR_NAME
    ```

    Shown here truncated, the output from the [az acr build][az-acr-build] command is similar to the following. You can see the packaging of the source code into a tarball, its upload to Azure, and the details of the `docker build` operation occurring in the cloud.

    ```console
    $ az acr build -t helloacrbuild:v1 --context . --registry $ACR_NAME
    Starting to archive the source code to '/tmp/source_archive_-5931255349851486748.tar.gz'.
    The source code tarball file size is 22518 bytes.
    Queued a build with build-id: eastus-1.
    Starting to stream the logs...
    time="2018-04-03T22:19:15Z" level=info msg="Running command docker login -u 00000000-0000-0000-0000-000000000000 --password-stdin mycontainerregistry.azurecr.io"
    Login Succeeded
    time="2018-04-03T22:19:17Z" level=info msg="Running command docker build -f Dockerfile -t mycontainerregistry.azurecr.io/helloacrbuild:v1 ."
    Sending build context to Docker daemon  124.9kB

    [...]

    Build complete
    Build ID: eastus-1 was successful after 1m15.680657431s
    ```

## Deploy to Azure Container Instances

When you use ACR Build to build your container images, they're automatically pushed to your container registry, allowing you deploy them immediately after the build is complete. You don't have the extra step of pushing the image to a registry when you use ACR Build.

In this section, you create an Azure Key Vault and service principal, then deploy the container to Azure Container Instances (ACI) using the service principal's credentials.

### Configure registry authentication

All production scenarios should use [service principals][service-principal-auth] for access to an Azure container registry. Service principals allow you to provide role-based access control to your container images. For example, you can configure a service principal with pull-only access to a registry.

#### Create key vault

If you don't already have a vault in [Azure Key Vault](/azure/key-vault/), create one with the Azure CLI using the following [az keyvault create][az-keyvault-create] command.

Specify a name for your new key vault in `AKV_NAME`. The vault name must be unique within Azure, and must be 3-24 alphanumeric characters in length.

```sh
AKV_NAME=mykeyvault # Must be unique within Azure

az keyvault create -g $RES_GROUP -n $AKV_NAME
```

#### Create service principal and store credentials

You now need to create a service principal and store its credentials in your key vault.

The following command uses [az ad sp create-for-rbac][az-ad-sp-create-for-rbac] to create the service principal, and [az keyvault secret set][az-keyvault-secret-set] to store the service principal's **password** in the vault.

```sh
# Create service principal, store its password in AKV (the registry *password*)
az keyvault secret set \
  --vault-name $AKV_NAME \
  --name $ACR_NAME-pull-pwd \
  --value $(az ad sp create-for-rbac \
                --name $ACR_NAME-pull \
                --scopes $(az acr show --name $ACR_NAME --query id --output tsv) \
                --role reader \
                --query password \
                --output tsv)
```

The `--role` argument in the preceding command configures the service principal with the *reader* role, which grants it pull-only access to the registry. To grant both push and pull access, change the `--role` argument to *contributor*.

Next, store the service principal's *appId* in the vault, which is the **username** you pass to Azure Container Registry for authentication.

```sh
# Store service principal ID in AKV (the registry *username*)
az keyvault secret set \
    --vault-name $AKV_NAME \
    --name $ACR_NAME-pull-usr \
    --value $(az ad sp show --id http://$ACR_NAME-pull --query appId --output tsv)
```

You've created an Azure Key Vault and stored two secrets in it:

* `$ACR_NAME-pull-usr`: The service principal ID, for use as the container registry **username**.
* `$ACR_NAME-pull-pwd`: The service principal password, for use as the container registry **password**.

You can now reference these secrets by name when you or your applications and services pull images from the registry.

### Deploy container with Azure CLI

Now that the service principal credentials are stored as Azure Key Vault secrets, your applications and services can use them to access your private registry.

Execute the following [az container create][az-container-create] command to deploy a container instance. The command uses the service principal's credentials stored in Azure Key Vault to authenticate to your container registry.

```sh
az container create \
    --name aci-demo \
    --resource-group $RES_GROUP \
    --image $ACR_NAME.azurecr.io/helloacrbuild:v1 \
    --registry-login-server $ACR_NAME.azurecr.io \
    --registry-username $(az keyvault secret show --vault-name $AKV_NAME -n $ACR_NAME-pull-usr --query value -o tsv) \
    --registry-password $(az keyvault secret show --vault-name $AKV_NAME -n $ACR_NAME-pull-pwd --query value -o tsv) \
    --dns-name-label aci-demo-$RANDOM \
    --query ipAddress.fqdn
```

The `--dns-name-label` value must be unique within Azure, so the preceding command appends a random number to the container's DNS name label. The output from the command displays the container's fully qualified domain name (FQDN), for example:

```console
$ az container create --name aci-demo --resource-group $RES_GROUP --image $ACR_NAME.azurecr.io/aci-helloacrbuild:v1 --registry-login-server $ACR_NAME.azurecr.io --registry-username $(az keyvault secret show --vault-name $AKV_NAME -n $ACR_NAME-pull-usr --query value -o tsv) --registry-password $(az keyvault secret show --vault-name $AKV_NAME -n $ACR_NAME-pull-pwd --query value -o tsv) --dns-name-label aci-demo-$RANDOM --query ipAddress.fqdn
"aci-demo-25007.eastus.azurecontainer.io"
```

### Verify deployment

To watch the startup process of the container, use the [az container attach][az-container-attach] command. The `az container attach` first displays the container's status as it pulls its image and starts, then binds your local console's STDOUT and STDERR to that of the container's.

```sh
az container attach -g $RES_GROUP -n aci-demo
```

Output from the `az container attach` command should appear similar to the following:

```console
$ $ az container attach -g $RES_GROUP -n aci-demo
Container 'aci-demo' is in state 'Running'...
(count: 1) (last timestamp: 2018-04-03 19:45:37+00:00) pulling image "acr11292.azurecr.io/helloacrbuild:v1"
(count: 1) (last timestamp: 2018-04-03 19:45:44+00:00) Successfully pulled image "acr11292.azurecr.io/helloacrbuild:v1"
(count: 1) (last timestamp: 2018-04-03 19:45:44+00:00) Created container with id 094ab4da40138b36ca15fc2ad5cac351c358a7540a32e22b52f78e96a4cb3413
(count: 1) (last timestamp: 2018-04-03 19:45:44+00:00) Started container with id 094ab4da40138b36ca15fc2ad5cac351c358a7540a32e22b52f78e96a4cb3413

Start streaming logs:
listening on port 80
```

When you see "listening on port 80", which is output from the container's STDOUT, the application has started and you can navigate to the container's FQDN to view it. To detach your console from the container, hit `Control+C`.

## Clean up resources

To remove all resources you've created in this quickstart, including the container, container registry, key vault, and service principal, issue the following commands:

```sh
az group delete -g $RES_GROUP
az ad sp delete --id http://$ACR_NAME-pull
```

## Next steps

Now that you've tested your inner loop, configure a **build task** that can be triggered by source code commits or base image updates:

[Configure a build task](quickstart-buildtask.md)

<!-- docs.microsoft.com ONLY
> [!div class="nextstepaction"]
> [Configure a build task][quickstart-buildtask.md]
-->

<!-- LINKS -->
[az-acr-build]: https://docs.microsoft.com/cli/azure/acr#az-acr-build
[az-ad-sp-create-for-rbac]: https://docs.microsoft.com/cli/azure/ad/sp#az-ad-sp-create-for-rbac
[az-container-attach]: https://docs.microsoft.com/cli/azure/container#az-container-attach
[az-container-create]: https://docs.microsoft.com/cli/azure/container#az-container-create
[az-keyvault-create]: https://docs.microsoft.com/cli/azure/keyvault/secret#az-keyvault-create
[az-keyvault-secret-set]: https://docs.microsoft.com/cli/azure/keyvault/secret#az-keyvault-secret-set
[service-principal-auth]: https://docs.microsoft.com/azure/container-registry/container-registry-auth-service-principal
[terms-of-use]: https://azure.microsoft.com/support/legal/preview-supplemental-terms/