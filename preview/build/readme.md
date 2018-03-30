# ACR Build Preview information

## Announcements
We're happy to release Preview 2 which includes:

- build-task: a build definition that can be triggered on git commits, or run directly
- build-task logs: which lists the logs, their status and can connect to currently running build

## Things that are coming soon

- Base image updates: this was the main scenario we focus upon for ACR build. We'll have more on this soon. For background, you can read [OS & Framework Patching with Docker Containers – a paradigm shift](https://blogs.msdn.microsoft.com/stevelasker/2017/12/20/os-framework-patching-with-docker-containers-paradigm-shift/)
- Build Caching: we know this is the most painful part of ACR Build; waiting for image pulls and benefiting from interim image cache. 

## Getting access to ACR Build Preview

Request early access to ACR Build at: https://aka.ms/acr/preview/signup


> For Preview 2, ACR Build is currently available in EastUS. 

## Providing Feedback

We're currently in private preview, which is why our docs are hosted here. 
To discuss ACR Build with the product team and others within the preview, once you're signed up and given access, you can find us at: http://azurecr.slack.com/

## Try ACR with Cloud Shell

- Launch Cloud shell [http://shell.azure.com](https://shell.azure.com)


- Remove any previous versions and install the current extension

```
az extension remove -n acrbuildext
az extension add --source https://acrbuild.blob.core.windows.net/cli/acrbuildext-0.0.2-py2.py3-none-any.whl -y
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
git clone https://github.com/SteveLasker/node-helloworld.git
cd node-helloworld
az acr build -t helloworld:v1 --context . -r $ACR_NAME
```

## Next Steps

For a full walkthrough, please see: [acr build quickstart](./quickstart-acrbuild.md)

