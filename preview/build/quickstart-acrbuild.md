# ACR Build for Quick Builds
With ACR Build, you can extend your inner loop to the cloud, validating your build will work once your code is checked in. ACR Build also enables you to work locally, without the docker client as you can take your source, build in Azure and test a deployment. 

Once you've tested your inner loop, [configure a build task](./quickstart-buildtask.md) which can be triggered by SCC commits, or base image updates. 

## Getting Access to ACR Build
- Request access to ACR Build Preview https://aka.ms/acr/preview/signup

- [Install the preview az acr build cli](../install.md)

## Test Locally (using Docker for Windows/Mac)
To see a quick example, we'll clone a repo, build it locally, and compare with ACR Build in Azure, testing with Azure Container Instances (ACI)

- Clone the sample repo

    ```
    git clone https://github.com/SteveLasker/aspnetcore-helloworld.git
    ```

- Enter the directory
    
    ```
    cd aspnetcore-helloworld
    ```

- Build locally - *note this step is optional*, only used as a comparison with `az acr build`. If you don't have docker installed or running locally, you can skip to **Testing in Azure**
    
    ```
    docker build -t helloworld:v1 -f HelloWorld/Dockerfile .
    ```

- Run the image

    ```
    docker run -D -p 8088:80 helloworld:v1
    ```

- Browse the site: 

    ```
    http://localhost:8088
    ```

## Building in Azure

In the following example, I create a registry named **jengademos**. This registry name will be taken. Replace **jengademos** with your own registry. 

***Note: for Preview, only registries in EastUS are supported***

- Create a registry
    
    ```
    ACR_NAME=jengademos
    az group create -l eastus -g $ACR_NAME
    az acr create -g $ACR_NAME --sku Standard -n $ACR_NAME
	```

- Build the image:

    ```
    az acr build -t helloworld:v1 -f ./HelloWorld/Dockerfile --context . --registry $ACR_NAME 
    ```

## Deploy to ACI
As we continue to integrate ACI into end to end workflows, we're working through more production grade examples for authenticating between ACI and ACR. In this example, we use Azure Keyvault to store the user/password required to access ACR. 

- Create a Keyvault to store the Username/Password for access to all your registries within ACR.

    `az keyvault create -g $ACR_NAME -n acr`
	
- Create a service principal for use by any service that requires registry access, storing the user/pwd values in keyvault for current and future reference
    - create a service principal, saving the password

    ```
    az keyvault secret set --vault-name acr \
      --name $ACR_NAME-pull-pwd \
      --value $(az ad sp create-for-rbac \
      --name $ACR_NAME-pull \
      --scopes $(az acr show --name $ACR_NAME --query id --output tsv) \
                    --role reader \
                    --query password \
                    --output tsv)
    ```

	- Set the pull-usr based on the Service Principal AppId

    ```
    az keyvault secret set --vault-name acr \
      --name $ACR_NAME-pull-usr \
      --value $(az ad sp show --id http://$ACR_NAME-pull --query appId --output tsv)
    ```

	- With service principal credentials saved in keyvault, create an ACI instance

    ```
    az container create --name jengademo -g jengademos1 -l eastus \
       --image $ACR_NAME.azurecr.io/helloworld:v1 \
       --registry-login-server $ACR_NAME.azurecr.io \
       --registry-username $(az keyvault secret show --vault-name acr -n $ACR_NAME-pull-usr --query value -o tsv) \
       --registry-password $(az keyvault secret show --vault-name acr -n $ACR_NAME-pull-pwd --query value -o tsv) \
       --dns-name-label aci-demo \
       -o json
    ```

	Watch the creation of ACI, awaiting the public IP

    ```
    watch az container show  --name jengademo -g jengademos
    ```

	Browse ACI

## Cleaning up

```
az group delete -g $ACR_NAME
az ad sp delete --id http://$ACR_NAME-pull
```
