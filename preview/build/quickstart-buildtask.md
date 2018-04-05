---
title: Create a build tasks for automated container image builds in Azure Container Registry
description: Learn how to build Docker container images in Azure with Azure Container Registry Build (ACR Build), then deploy it to Azure Container Instances.
services: container-registry
author: mmacy
manager: timlt

ms.service: container-registry
ms.topic: article
ms.date: 04/05/2018
ms.author: marsma
---

# Automate container image builds with Azure Container Registry Build

In addition to [Quick Build](quickstart-acrbuild.md), ACR Build supports automated Docker container image builds with the *Build Task*. In this article, you use the Azure CLI to configure a build task to automatically trigger container image builds in the cloud when you commit source code to a Git repository.

## Prerequisites

To complete the steps in this article, you must have the following:

* [Access](https://aka.ms/acr/preview/signup) to the ACR Build preview
* [Azure CLI][azure-cli] with the [ACR Build extension](../install.md) installed
* [GitHub](github.com) user account

## Build Task

A Build Task defines the properties of an automated build, including the location of the container image source code and the event that triggers the build. When an event defined in the Build Task occurs, such as a commit to a Git repository, ACR Build initiates a container image build in the cloud, and by default, pushes a succesfully built image to the Azure container registry specified in the task.

ACR Build currently supports the following build triggers:

* Commit to a Git repository

> Git commit-triggered build tasks currently support only GitHub-based personal access tokens (PAT). VSTS token support is planned for a future update.

Support for these build task triggers is planned, but **not yet implemented**:

* Update to a base container image
* Webhook event
* Azure Event Grid notification

## Create a Build Task

Build tasks define the conditions under which a build should be triggered

### Create a GitHub personal access token

To trigger a build on a commit to a Git repository, ACR Build must be able to access the repository. You can provide this access with a personal access token you generate in GitHub.

1. Create a Github Personal Access Token
    - Create a github token by navigating to:
        https://github.com/settings/tokens/new
    - Under repo, enable repo:status, public_repo

        ![](./media/CreateGithubToken.png)

    - Copy the generated token

### Create a Build Task

Create a build task, which is automatically triggered on scc commits.

With the GitHub PAT, execute the following command replacing the context with your github:

```
az acr build-task create --name helloworld -r $ACR_NAME \
    -t helloworld:v1 \
    --context https://github.com/SteveLasker/acrbuild-node-helloworld --git-access-token [yourToken]
```

> Note: Setting the :tag to :{build.Id} will be implemented in a future preview

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

## Next steps

Your feedback on [azurecr.slack.com](https://azurecr.slack.com) would be helpful.

<!-- LINKS -->
[azure-cli]: https://docs.microsoft.com/cli/azure/install-azure-cli


<!-- NOT YET IMPLEMENTED
### Specify a sub folder

We're exploring the same convention as the [docker cli](https://docs.docker.com/engine/reference/commandline/build/#git-repositories), to specify the branch and sub folder as well.

```
az acr build-task create --name helloworld -n jengademos \
    -t helloworld:v1  \
    --context https://github.com/SteveLasker/acrbuild-node-helloworld.git$subBranch:subFolder --git-access-token [yourToken]
```

ACR Build supports several triggers that can initiate an build:

* Commit to a Git repository<sup>1</sup>
* Update to a base container image<sup>2</sup>
* Webhook<sup>2</sup>
* Azure Event Grid notification<sup>2</sup>

<sup>1</sup> ACR Build currently supports only GitHub-based personal access tokens (PAT) for Git commit-triggered builds. VSTS token support is planned for a future update.<br/>
<sup>2</sup> Support for these build task triggers is planned, but not yet implemented.
-->