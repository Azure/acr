---
title: Create a build tasks for automated container image builds in Azure Container Registry
description: Learn how to build Docker container images in Azure with Azure Container Registry Build (ACR Build), then deploy it to Azure Container Instances.
services: container-registry
author: mmacy
manager: timlt

ms.service: container-registry
ms.topic: article
ms.date: 04/03/2018
ms.author: marsma
---

# Preview 2 Enables Build-Tasks
With preview 2, you can now create and save a build-task. Which is the definition of a build.

# Setup Automated Build

ACR Build-tasks can be triggered on:
- git commits
- base image updates *
- Webhooks *
- Azure Evengtrid notifications *

    \* indicates events not yet supported

> ACR Build currently supports github based PAT tokens. VSTS tokens will come in a future preview

## Create a Github Personal Access Token
- Create a github token by navigating to:
    https://github.com/settings/tokens/new
- Under repo, enable repo:status, public_repo

    ![](./media/CreateGithubToken.png)

- Copy the generated token

## Create a build task, which is automatically triggered on scc commits.

With the git PAT, execute the following command replacing the context with your github:

```
az acr build-task create --name helloworld -r $ACR_NAME \
    -t helloworld:v1 \
    --context https://github.com/SteveLasker/acrbuild-node-helloworld --git-access-token [yourToken]
```

> Note: Setting the :tag to :{build.Id} will be implemented in a future preview

### Specifying a sub folder

We're exploring the same convention as the [docker cli](https://docs.docker.com/engine/reference/commandline/build/#git-repositories), to specify the branch and sub folder as well.

```
az acr build-task create --name helloworld -n jengademos \
    -t helloworld:v1  \
    --context https://github.com/SteveLasker/acrbuild-node-helloworld.git$subBranch:subFolder --git-access-token [yourToken]
```
Your feedback on [azurecr.slack.com](https://azurecr.slack.com) would be helpful...

## Trigger a build

You can trigger a build with a SCC commit, but we can also manually trigger a build
```
az acr build-task run --name helloworld -r $ACR_NAME
```

# View Build Status
There are several commands to view the status of a build-task, as well as the logs, including live-streaming the most recent/current build log available through the `build-task logs` parameter

## Trigger a build, view the status
Using the --no-logs, trigger a build. Then, use the `build-task logs` parameter to view the current log
```
az acr build-task run --name helloworld --no-logs -r $ACR_NAME
az acr build-task logs -r $ACR_NAME
```

> Note: in a future preview, `--name helloworld` will limit displaying the most recent build log to a specific build-task

## List the build-tasks For a Registry
```
az acr build-task list -r $ACR_NAME
```

## List the builds that have been executed, or executing for a registry
```
az acr build-task list-builds -r $ACR_NAME
```

## List the builds for a build-task within a registry
```
az acr build-task list-builds --name helloworld -r $ACR_NAME
```
> Note: preview 2 is outputting additional data in the table output that will be scrubbed in a future preview

## Show the last (or current) log for a build-task
```
az acr build-task logs -r $ACR_NAME
```

## Show the last (or current) log for a build-task
```
az acr build-task logs --name helloworld -r $ACR_NAME
```
> Note: log filtering to a build-task is not yet implemented

## Show the log for a specific build
```
az acr build-task logs --build-id eus-1 -r $ACR_NAME
```

## Manually trigger the build

```
az acr build-task run --name helloworld -r $ACR_NAME
```
