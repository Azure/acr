---
title: ACR Task Reference
description: Reference for ACR Tasks and their execution model
services: container-registry
author: stevelas
manager: balans

ms.service: container-registry
ms.topic: article
ms.date: 08/31/2018
ms.author: stevelas
---
# ACR Tasks Reference

ACR Tasks provide a container centric compute primitive, focused on building and patching containers.
This doc cover the basic commands, parameters, syntax and brief examples.

## ACR Task Execution Model

ACR Tasks take advantage of the container execution and isolation model, enabling customers to run any series of containers as functions across a common directory. ACR Tasks provide a common context and conditional/dependency flow between steps providing primitive, yet robust scenarios. By deferring the execution to containers you provide, ACR Tasks has minimal dependencies between the Task execution environment and your code. Using this pattern, developers may use any language or framework they desire, running on Linux or Windows operating systems, minimizing version dependency situations. 


- **[cmd](#cmd)** to run a container as a function, enabling parameters passed to the container [ENTRYPOINT]. `cmd` supports  run parameters including ports, volumes and other familiar `docker run` parameters, enabling unit and functional testing with concurrent container execution. 
- **[build](#build)** containers using familiar syntax of `docker build`
- **[push](#push)** newly built images to a registry, including ACR, Docker hub and other private registries.

## cmd

`cmd` is the most basic execution of a container. It follows the format of
```yaml
version: 1.0-preview-1
steps:
    - [cmd]: [containerImage] [cmdParameters to the image]
      [propety]: [value]
```
Using cmd, ACR Tasks run an image as a function. 

The most basic hello-world example would be:

```yaml
version: 1.0-preview-1
steps:
    cmd: hello-world
```

```sh
az acr task run -f hello-world.yaml https://github.com/AzureCR/acr-tasks-sample.git
```
```
Hello from Docker!
This message shows that your installation appears to be working correctly.
...
2018/09/01 16:05:07 Step ID acb_step_0 marked as successful (elapsed time in seconds: 7.6)

Build ID: ba2n was successful after 10.3 seconds
```


The following task.yaml will instance the [bash](https://hub.docker.com/_/bash/) image, hosted on docker hub, executing `echo hello world`
```yaml
version: 1.0-preview-1
steps:
    - cmd: bash echo hello world
```
To test this example

```sh
az acr task run -f bash-echo.yaml https://github.com/AzureCR/acr-tasks-sample.git
```

### Versioning

To version the function, use version specific tags. The following example executes the [bash:3.0] (https://hub.docker.com/_/bash/) image:
```yaml
version: 1.0-preview-1
steps:
    - cmd: bash:3.0 echo hello world
```
To test this example

```sh
az acr task run -f bash-echo-3.yaml https://github.com/AzureCR/acr-tasks-sample.git
```

### Custom Images

When executing `cmd`, the image reference follows the standard convention for docker run. Images not prefaced with a registry are assumed to originate from docker.io. The above example could equally be represented as:
```yaml
version: 1.0-preview-1
steps:
    - cmd: docker.io/bash:3.0 echo hello world
```

By using docker run conventions, you can run any image, in any registry, private or public. Images referenced in the same registry ACR Task is executing will not require additional credentials. 

To run the bash image from your ACR. Replace [yourregistry] with the name of your registry. 
```yaml
version: 1.0-preview-1
steps:
    - cmd: [yourregistry].azurecr.io/bash:3.0 echo hello world
```

```sh
az acr task run -f bash-echo.yaml https://github.com/AzureCR/acr-tasks-sample.git
```

### Run Properties

ACR Tasks supports a set of [built-in properties](#properties). Using a dot notation, you can navigate read-only properties and values passed in.

To generalize a task.yaml file for your registry, change specific registry references to use the `.Run.Registry` syntax.
```yaml
version: 1.0-preview-1
steps:
    - cmd: {{.Run.Registry}}/bash:3.0 echo hello world
```

### cmd Properties
Supported cmd properties include:
- [detach: bool (optional)](#detach)
- [entryPoint: string (optional)](#entryPoint)
- [env: [string, string, ...] (optional)](#env)
- [exitedWith: [int, int, ...] (optional)](#exitedWith)
- [exitedWithout: [int, int, ...] (optional)](#exitedWithout)
- [id: string (optional)](#id)
- [ignoreErrors: bool (optional)](#ignoreErrors)
- [keep: bool (optional)](#keep)
- [ports: [string, string, ...] (optional)](#ports)
- [startDelay: int (in seconds) (optional)](#startDelay)
- [timeout: int (in seconds) (optional)](#timeout)
- [when: [string, string, ...] (optional)](#when)
- [workingDirectory: string (optional)](#workingDirectory)

## build
`build` lifts `cmd: docker build` as as a first class primitive. `build:` follows the following syntax:

```yaml
version: 1.0-preview-1
steps:
    - [build]: -t [imageName] [context]
      [property]: [value]
```

To build a hello world image:

```yaml
version: 1.0-preview-1
steps:
  - build: -t {{.Run.Registry}}/hello-world -f hello-world.dockerfile .
```

```sh
az acr task run -f build-hello-world.yaml https://github.com/AzureCR/acr-tasks-sample.git
```


### -t | --image (optional)
Define the fully qualified image:tag
As images may be used for inner task validations, not all images may be pushed. If 
Note, tagging a

### Build Properties
Supported cmd properties include:
- [detach: bool (optional)](#detach)
- [entryPoint: string (optional)](#entryPoint)
- [env: [string, string, ...] (optional)](#env)
- [exitedWith: [int, int, ...] (optional)](#exitedWith)
- [exitedWithout: [int, int, ...] (optional)](#exitedWithout)
- [id: string (optional)](#id)
- [ignoreErrors: bool (optional)](#ignoreErrors)
- [keep: bool (optional)](#keep)
- [ports: [string, string, ...] (optional)](#ports)
- [startDelay: int (in seconds) (optional)](#startDelay)
- [timeout: int (in seconds) (optional)](#timeout)
- [when: [string, string, ...] (optional)](#when)
- [workingDirectory: string (optional)](#workingDirectory)
## push


## Build, Push, Run hello-world

To build and run hello-world:
```yaml
version: 1.0-preview-1
steps:
  - build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} -f hello-world.dockerfile .
  - push: {{.Run.Registry}}/hello-world:{{.Run.ID}}
  - cmd: {{.Run.Registry}}/hello-world:{{.Run.ID}}
```
Run the following:
```sh
az acr task run -f build-run-hello-world.yaml https://github.com/AzureCR/acr-tasks-sample.git
```
az acr build -f build-run-hello-world.yaml https://github.com/AzureCR/acr-tasks-sample.git



# ACR Task Step Properties
## detach:
## entryPoint:
## env:
## exitedWith:
## exitedWithout:
## id:
## ignoreErrors:
## keep:
## ports:
## startDelay:
## timeout:
## when:

## workingDirectory:
        
## Build variables

The following variables can be accessed using `{{ .Run.VariableName }}`, where `VariableName` equals one of the following:

- ID
- Commit
- Repository
- Branch
- TriggeredBy
- Registry
- GitTag
- Date
- SharedContextDirectory

> [!div class="nextstepaction"]
* TBD: