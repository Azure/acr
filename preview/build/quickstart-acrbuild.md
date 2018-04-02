# ACR Build for quick builds

ACR Build extends your inner loop to the cloud, validating build success once your code is checked in. ACR Build also enables you to work locally, without the Docker client: build your source in Azure, then test it with a deployment to the cloud.

## Get ACR Build

* **Access**: While ACR Build is in preview, you must first request access at https://aka.ms/acr/preview/signup

* **Installation**: Next, [install the ACR Build](../install.md) preview, which enables the `az acr build` command in the Azure CLI.

## Test locally with Docker for Windows/Mac
To see a quick example, we'll clone a repo and build it locally. In the next section, we'll compare by using ACR Build in Azure, then deploy to Azure Container Instances (ACI).

1. Clone the sample repo

    ```sh
    git clone https://github.com/SteveLasker/aspnetcore-helloworld.git
    ```

1. Enter the directory

    ```sh
    cd aspnetcore-helloworld
    ```

1. (OPTIONAL) Build and run locally. This step is used only as a comparison with `az acr build`. If you don't have Docker installed or running locally, you can skip this step and move on to [Building in Azure](#building-in-azure).

    ```sh
    # Build image locally
    docker build -t helloworld:v1 -f HelloWorld/Dockerfile .
    # Run the image
    docker run -d -p 8088:80 helloworld:v1
    ```

    Browse the locally running application: http://localhost:8088

## Building in Azure

In the following example, I create a registry named **jengademos**. This registry name will be taken. Replace **jengademos** with the name of your own Azure container registry.

<<<<<<< HEAD
> NOTE: ACR Build Preview 2 supports only those registries in **EastUS**.

1. Create a registry and log in to it with your Azure ID:

    ```sh
    RES_GROUP=myresourcegroup # Resource Group name
    ACR_NAME=jengademos
    az group create -l eastus -g $RES_GROUP
    az acr create -g $RES_GROUP --sku Standard -n $ACR_NAME
    az acr login -n $ACR_NAME
=======
***Note: for Preview, only registries in EastUS are supported***

- Create a registry
    
    ```
    ACR_NAME=jengademos
    az group create -l eastus -g $ACR_NAME
    az acr create -g $ACR_NAME --sku Standard -n $ACR_NAME
>>>>>>> f71e02c05a5ceb181c381c9bcaf1de5e43a47192
	```

1. Build the image with **ACR Build**:

    ```sh
    az acr build -t helloworld:v1 -f ./HelloWorld/Dockerfile --context . --registry $ACR_NAME
    ```

## Deploy to ACI
As we continue to integrate ACI into end to end workflows, we're working through more production-grade examples for authenticating between ACI and ACR. In this example, we use Azure Key Vault to store the user/password required to access ACR.

### Configure registry authentication

In any production scenario, access to an Azure container registry should be provided by using [service principals](../container-registry/container-registry-auth-service-principal.md). Service principals allow you to provide role-based access control to your container images. For example, you can configure a service principal with pull-only access to a registry.

In this section, you create an Azure key vault and a service principal, and store the service principal's credentials in the vault.

#### Create key vault

If you don't already have a vault in [Azure Key Vault](/azure/key-vault/), create one with the Azure CLI using the following commands.

Specify a name for your new key vault in `AKV_NAME`. The vault name must be unique within Azure and must be 3-24 alphanumeric characters in length, begin with a letter, end with a letter or digit, and cannot contain consecutive hyphens.

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
    --image $ACR_NAME.azurecr.io/helloworld:v1 \
    --registry-login-server $ACR_NAME.azurecr.io \
    --registry-username $(az keyvault secret show --vault-name $AKV_NAME -n $ACR_NAME-pull-usr --query value -o tsv) \
    --registry-password $(az keyvault secret show --vault-name $AKV_NAME -n $ACR_NAME-pull-pwd --query value -o tsv) \
    --dns-name-label aci-demo-$RANDOM \
    --query ipAddress.fqdn
```

The `--dns-name-label` value must be unique within Azure, so the preceding command appends a random number to the container's DNS name label. The output from the command displays the container's fully qualified domain name (FQDN), for example:

```console
$ az container create --name aci-demo --resource-group $RES_GROUP --image $ACR_NAME.azurecr.io/aci-helloworld:v1 --registry-login-server $ACR_NAME.azurecr.io --registry-username $(az keyvault secret show --vault-name $AKV_NAME -n $ACR_NAME-pull-usr --query value -o tsv) --registry-password $(az keyvault secret show --vault-name $AKV_NAME -n $ACR_NAME-pull-pwd --query value -o tsv) --dns-name-label aci-demo-$RANDOM --query ipAddress.fqdn
"aci-demo-25007.eastus.azurecontainer.io"
```

### Verify deployment

To watch the startup process of the container, use the [az container attach][az-container-attach] command. The `az container attach` first displays the container's status as it pulls its image and starts, then binds your local console's STDOUT and STDERR to that of the container's.

```sh
az container attach -g $RES_GROUP -n aci-demo
```

Output from the `az container attach` command should appear similar to the following:

```console
$ az container attach -g $RES_GROUP -n aci-demo
Container 'aci-demo' is in state 'Running'...
(count: 1) (last timestamp: 2018-04-02 20:26:23+00:00) pulling image "jengademos.azurecr.io/helloworld:v1"
(count: 1) (last timestamp: 2018-04-02 20:26:40+00:00) Successfully pulled image "jengademos.azurecr.io/helloworld:v1"
(count: 1) (last timestamp: 2018-04-02 20:26:40+00:00) Created container with id 971fe0a761c9d932071d2fe4bdf374ab712bb6d6e8d077edaa7a1e266a421bba
(count: 1) (last timestamp: 2018-04-02 20:26:40+00:00) Started container with id 971fe0a761c9d932071d2fe4bdf374ab712bb6d6e8d077edaa7a1e266a421bba

Start streaming logs:
warn: Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[35]
      No XML encryptor configured. Key {73c045ae-b90a-4d6c-ad92-54f1ffb18c37} may be persisted to storage in unencrypted form.
Hosting environment: Production
Content root path: /app
Now listening on: http://[::]:80
Application started. Press Ctrl+C to shut down.
```

When you see "Application started. Press Ctrl+C to shut down.", the application has started and you can navigate to the container's FQDN to view it. To detach from the container, hit `Control+C`.

> NOTE: The last line in the output, "`Press Ctrl+C to shut down.`", is output from the *container's* STDOUT. By pressing *Control+C*, you exit the `az container attach` command. The container and its application continues to run.

## Clean up resources

To remove all resources you've created in this quickstart, including the container, container registry, key vault, and service principal, issue the following commands:

```sh
az group delete -g $RES_GROUP
az ad sp delete --id http://$ACR_NAME-pull
```

## Next steps

Now that you've tested your inner loop, [configure a build task](./quickstart-buildtask.md) that can be triggered by SCC commits or base image updates.

<!-- LINKS -->
[az-ad-sp-create-for-rbac]: https://docs.microsoft.com/cli/azure/ad/sp#az-ad-sp-create-for-rbac
[az-container-attach]: https://docs.microsoft.com/cli/azure/container#az-container-attach
[az-container-create]: https://docs.microsoft.com/cli/azure/container#az-container-create
[az-keyvault-secret-set]: https://docs.microsoft.com/cli/azure/keyvault/secret#az-keyvault-secret-set
