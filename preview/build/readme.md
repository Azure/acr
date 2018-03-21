# ACR Build Preview information

## Getting access to ACR Build Preview

Request early access to ACR Build at: https://aka.ms/acr/preview/signup


> For Preview 1, ACR Build is currently available in EastUS. 

## Providing Feedback

We're currently in private preview, which is why our docs are hosted here. 
To discuss ACR Build with the product team and others within the preview, once you're signed up and given access, you can find us at: http://azurecr.slack.com/

## Try ACR with Cloud Shell

- Launch Cloud shell [http://shell.azure.com](https://shell.azure.com)


```bash
mkdir ~/cli-extensions
cd ~/cli-extensions 
curl -O https://acrbuild.blob.core.windows.net/cli/acrbuildext-0.0.1-py2.py3-none-any.whl
az extension add --source ./acrbuildext-0.0.1-py2.py3-none-any.whl -y
az acr build --help
```

## Create an ACR instance in EastUS

If you already have ACR in the EastUS region, proceed to **Try acr build**

```
ACR_NAME=jengademos
az group create -l eastus -g $ACR_NAME
az acr create -g $ACR_NAME --sku Standard -n $ACR_NAME
```

## Try acr build

```bash
git clone https://github.com/SteveLasker/aspnetcore-helloworld.git
cd aspnetcore-helloworld
az acr build -t helloworld:v1 -f ./HelloWorld/Dockerfile --context . -r myregistry
```

## Next Steps

For a full walkthrough, please see: [acr build quickstart](./quickstart-acrbuild.md)

