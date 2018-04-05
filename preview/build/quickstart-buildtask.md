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

## Build task

A build task defines the properties of an automated build, including the location of the container image source code and the event that triggers the build. When an event defined in the build task occurs, such as a commit to a Git repository, ACR Build initiates a container image build in the cloud, and by default, pushes a successfully built image to the Azure container registry specified in the task.

ACR Build currently supports the following build triggers:

* Commit to a Git repository

> Git commit-triggered build tasks currently support only GitHub-based personal access tokens (PAT). VSTS token support is planned for a future update.

Support for these build task triggers is planned, but **not yet implemented**:

* Update to a base container image
* Webhook event
* Azure Event Grid notification

## Create a build task

Build tasks define the conditions under which a build should be triggered, the location of the source to build, and other properties unique to the build definition. In this section, you create a GitHub personal access token (PAT), then create a build task that will trigger a build when code is committed to a GitHub repository.

### Create a GitHub personal access token

To trigger a build on a commit to a Git repository, ACR Build must be able to access the repository. You can provide this access with a personal access token (PAT) you generate in GitHub.

1. Navigate to the PAT creation page on GitHub at https://github.com/settings/tokens/new
1. Enter a short **description** for the token, for example, "ACR Build Task Demo"
1. Under **repo**, enable **repo:status** and **public_repo**

   ![Screenshot of the Personal Access Token generation page in GitHub](./media/build-task-01-new-token.png)

1. Select the **Generate token** button (you may be asked to confirm your password)
1. Copy and save the generated token in a **secure location**. You'll use the token when you define a build task in a later step.

   ![Screenshot of the generated Personal Access Token in GitHub](./media/build-task-02-generated-token.png)

### Fork sample repository

Next, use the GitHub UI to fork the the sample repository into your GitHub account. ACR Build uses your GitHub PAT to not only query commit status, but also create a GitHub webhook for each build task.

Sample repository: https://github.com/Azure-Samples/aci-helloworld

![Screenshot of the Fork button (highlighted) in GitHub](./media/build-task-03-fork.png)

> NOTE: You can skip forking the sample repository if you have write access to a different GitHub repo. Because ACR Build needs to create webhooks in GitHub on your behalf, your account must have write access to the repo for which you'd like to create the build task.

### Create the build task

Now that you've completed the steps required for ACR Build to be able to read the commit status and create webhooks in a repository, you can create a build task that triggers a container image build on commits to the repo.

Execute the following [az acr build-task create][az-acr-build-task-create] command. Replace `<your-github-username>` with your GitHub username, and `<your-access-token>` with the PAT you generated in a previous step. If you haven't previously populated the `ACR_NAME` environment variable with the name of your Azure container registry (such as in the [previous tutorial](quickstart-acrbuild.md)), replace `$ACR_NAME` with the name of your registry.

```sh
az acr build-task create \
    --name buildhelloworld \
    -r $ACR_NAME \
    -t helloworld:v1 \
    --context https://github.com/<your-github-username>/aci-helloworld \
    --git-access-token <your-access-token>
```

This build task specifies that any time code is committed to the repository specified in the `--context` parameter, ACR Build will build the container image from the code in the repository.

> NOTE: Although not yet implemented, Azure Container Registry plans support for specifying parameters in the build task definition. For example, setting `:tag` to `:{build.Id}` to automatically tag an image with the ACR Build build ID.

Output from a successful [az acr build-task create][az-acr-build-task-create] command is similar to the following:

```console
$ az acr build-task create --name buildhelloworld -r $ACR_NAME -t helloworld:v1 --context https://github.com/githubuser/aci-helloworld --git-access-token 0123456789abcdef1234567890
{
  "additionalProperties": {},
  "alias": "buildhelloworld",
  "creationDate": "2018-04-05T17:26:14.347346+00:00",
  "id": "/subscriptions/<subscriptionID>/resourceGroups/myResourceGroup/providers/Microsoft.ContainerRegistry/registries/mycontainerregistry/buildTasks/buildhelloworld",
  "location": "eastus",
  "name": "buildhelloworld",
  "platform": {
    "additionalProperties": {},
    "cpu": 1,
    "osType": "Linux"
  },
  "provisioningState": "Succeeded",
  "resourceGroup": "myResourceGroup",
  "sourceRepository": {
    "additionalProperties": {},
    "isCommitTriggerEnabled": true,
    "repositoryUrl": "https://github.com/githubuser/aci-helloworld",
    "sourceControlAuthProperties": null,
    "sourceControlType": "Github"
  },
  "status": "enabled",
  "tags": null,
  "timeout": null,
  "type": "Microsoft.ContainerRegistry/registries/buildTasks"
}
```

## Trigger a build

You now have a build task that defines your build. To test the build definition, trigger a build manually by executing the [az acr build-task run][az-acr-build-task-run] command:

```sh
az acr build-task run --name buildhelloworld -r $ACR_NAME
```

Be default, the `az acr build-task run` command streams the log output to your console when you execute the command.

```sh
TODO: SHOW BUILD OUTPUT HERE
```

## View build status

There are several commands you can use to view build tasks, the status of a particular build task, as well as its logs.

## Trigger a build, view the status
Using the --no-logs, trigger a build. Then, use the `build-task logs` parameter to view the current log

```
az acr build-task run --name helloworld --no-logs -r $ACR_NAME
az acr build-task logs -r $ACR_NAME
```

> Note: in a future preview, `--name buildhelloworld` will limit displaying the most recent build log to a specific build-task

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
az acr build-task list-builds --name buildhelloworld -r $ACR_NAME
```
> Note: preview 2 is outputting additional data in the table output that will be scrubbed in a future preview

## Show the last (or current) log for a build-task
```
az acr build-task logs -r $ACR_NAME
```

## Show the last (or current) log for a build-task
```
az acr build-task logs --name buildhelloworld -r $ACR_NAME
```
> Note: log filtering to a build-task is not yet implemented

## Show the log for a specific build
```
az acr build-task logs --build-id eus-1 -r $ACR_NAME
```

## Manually trigger the build

```
az acr build-task run --name buildhelloworld -r $ACR_NAME
```

## Next steps

Your feedback on [azurecr.slack.com](https://azurecr.slack.com) would be helpful.

<!-- LINKS -->
[azure-cli]: https://docs.microsoft.com/cli/azure/install-azure-cli
[az-acr-build-task-create]: /cli/azure/acr#az-acr-build-task-create
[az-acr-build-task-run]: /cli/azure/acr#az-acr-build-task-run

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