## Test Locally

- Request access to ACR Build Preview https://aka.ms/acr/preview/signup

- [Install the Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-macos?view=azure-cli-latest)


- Clone the sample repo

    ```
    git clone https://github.com/SteveLasker/aspnetcore-helloworld.git
    ```

- Enter the directory
    
    ```
    cd aspnetcore-helloworld
    ```

- Build locally - note this step is optional, only used as a comparison with `az acr build`. If you don't have docker installed or running locally, you can skip to **Testing in Azure**
    
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

***Note: for Preview 1, only registries in EastUS are supported***

- Create a registry and login with your azure id
    
    ```
    ACR_NAME=jengademos
    az group create -l eastus -g $ACR_NAME
    az acr create -g $ACR_NAME --sku Standard -n $ACR_NAME
    az acr login -n $ACR_NAME
	```

- Build the image:

    ```
    az acr build -t helloworld:v1 -f ./HelloWorld/Dockerfile --context . --registry $ACR_NAME 
    ```

## Deploy to ACI

- Create a Keyvault to store the Username/Password for access to all your registries within ACR.

    `az keyvault create -g $ACR_NAME -n acr`
	
- Create a service principal for use by any service that requires registry access, storing the user/pwd values in keyvault for current and future reference

    ```bash
    az keyvault secret set --vault-name acr \
      --name $ACR_NAME-pull-pwd \
      --value $(az ad sp create-for-rbac \
      --name $ACR_NAME-pull \
      --scopes $(az acr show --name $ACR_NAME --query id --output tsv) \
                    --role reader \
                    --query password \
                    --output tsv)
    ```

	- Set the keyvault secrets

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



curl -O https://acrbuild.blob.core.windows.net/cli/acrbuildext-0.0.2-py2.py3-none-any.whl
az extension add --source ./acrbuildext-0.0.2-py2.py3-none-any.whl -y