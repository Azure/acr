---
title: ACR Task Walk Through
description: Walk through, using ACR Tasks
services: container-registry
author: stevelas
manager: balans

ms.service: container-registry
ms.topic: article
ms.date: 08/31/2018
ms.author: stevelas
---
# ACR Task Walk Through

ACR Tasks provide a container centric compute primitive, focused on building and patching containers.
This doc covers a walk through to understand the capabilities of ACR Tasks. 

## ACR Task Execution Model

ACR Tasks take advantage of the container execution and isolation model, enabling customers to run any series of containers as commands across a common directory. ACR Tasks provide a common context and conditional/dependency flow between steps providing primitive, yet robust scenarios. By deferring the execution to containers, ACR Tasks has minimal dependencies between the Task execution environment and the code within a container.

Using containers as a collection of commands; developers may use any language or framework they desire, running on Linux or Windows operating systems, minimizing version dependency. 

# Task Step Types
ACR Tasks supports three step types:
- **[build](#build)** containers using familiar syntax of `docker build`
- **[push](#push)** supports `docker push` of newly built or re-tagged images to a registry, including ACR, Docker hub and other private registries.
- **[cmd](#cmd)** to run a container as a command, enabling parameters passed to the containers `[ENTRYPOINT]`. `cmd` supports  run parameters including ports, volumes and other familiar `docker run` parameters, enabling unit and functional testing with concurrent container execution. 

# Running Samples

Samples referenced use `az acr run` and assume a default registry is configured.

- Configure a default registry

    Assuming your registry is named yourRegistry.azurecr.io, run the following
    ```sh
    az configure --defaults acr=yourRegistry
    ```

> **Note:** As of 9/9/18, `az acr run` is not yet public. Replace `az acr run` with `az acr build`, using the `-f` parameter to reference the `task.yaml` file.


## Building A Single Image

Using [ACR Build](https://aka.ms/acr/build), users can easily build and optionally push single images. 
```sh
az acr build -t hello-world:{{.Build.ID}} https://github.com/Azure-Samples/acr-build-helloworld-node.git
```

The equivalent ACR task would involve:
```yaml
version: 1.0-preview-1
steps:
  - build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} .
  - push: {{.Run.Registry}}/hello-world:{{.Run.ID}}
```

The task.yaml version does the following:

- breaks up build and push into separate steps
- changes `Build.ID` to [Run.ID](./container-registry-ref-acr-tasks-yaml.md#runid) to better represent a run, which may do many things, in addition to `docker build`
- provides a fully qualified refernce to the target registry using [Run.Registry](./container-registry-ref-acr-tasks-yaml.md#run.registry)

To test the above yaml, run the following command in [cloud shell](https://shell.azure.com) or any other bash environment. 

```sh

```

If the scenario required building a secondary test container, executing the test container before pushing the image, 


ACR Build supports multi-stage dockerfiles, however they 

> [!div class="nextstepaction"]
* TBD: