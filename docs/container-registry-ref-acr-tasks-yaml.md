---
title: ACR task.yaml Reference
description: Reference for ACR task.yaml formatting
services: container-registry
author: stevelas
manager: balans

ms.service: container-registry
ms.topic: article
ms.date: 08/31/2018
ms.author: stevelas
---
# ACR task.yaml Reference

ACR Tasks provide a container centric compute primitive, focused on building and patching containers.
This doc covers the commands and parameter syntax for tasks.yaml.

See [Task Overview](./container-registry-tasks-overview.md) for an overview of Tasks and how to execute `task.yaml`

## task.yaml Format
ACR Tasks supports multi-step declaration through standard yaml syntax. 

task.yaml supports:
- [Task Properties](#task-properties) which apply to the entire task execution, including [version](#version), [stepTimeout](#steptimeout) and [totalTimeout](#totaltimeout). 
- [Task Step Types](#task-step-types) which represent executing a container, [cmd](#cmd), [build](#build) a container and [push](#push)ing to push a newly built or retagged image.
- [Task Step Properties](#task-step-properties) are parameters applicable to each step, such as [startDelay](#startdelay) and [when](#when)

```yaml
version: # task.yaml format version
stepTimeout: # seconds each step may take
totalTimeout: # total seconds all steps must complete within.]
steps: # collection of executed container capabilities 
    cmd: # executes a cotnainer, using the [ENTRYPOINT] and parameters
      startDelay: # properties of the step, with this being the number of seconds to wait before beginning
    build: # equivalent to docker build, in a multi-tenant environment
    push: # push a newly built image
      when: # a means to defined parallel or dependent execution
```
> Note: **taska.yaml** follows strict yaml formating, including multi-line capabilities like `>` and `|`. If task execution fails, check the validity of the formatting, including nesting and usage of `:` to define each identifier. 

# Running Samples

Samples referenced use `az acr run` and assume a default registry is configured.

- Configure a default registry

    Assuming your registry is named yourRegistry.azurecr.io, run the following
    ```sh
    az configure --defaults acr=yourRegistry
    ```

> **Note:** As of 9/9/18, `az acr run` is not yet public. Replace `az acr run` with `az acr build`, using the `-f` parameter to reference the `task.yaml` file.


# Task Properties
Tasks have root properties that apply to the entire execution of the Task. Some properties may be overriden in a specific step. 

## version:
The version of the task.yaml file, as parsed by the ACR Tasks service.  

ACR Tasks will make every reasonable attempt to maintain backwards compatibility. Version values will allow ACR Tasks to adhere to compatibility within a defined version.

> Note: As of Preview 1, the version property is not yet required. However, it's highly recommended to use a specific version to avoid guessing which version the task.yaml file was intended to operate against.

- `1.0-preview-1` - Pre-release, preview 1. As this is the first public version of ACR Tasks, based on feedback, we may make breaking changes. However, this version will be supported with a yet to be determined time frame, as the 1.0 public release is made. 

## stepTimeout: 
Default Value: ___ [TODO:]

The maximum number of seconds an individual step has to execute. This property can be overridden by setting the [timeout](#timeout) property on a specific step.

## totalTimeout
The maximum number of seconds all steps must execute within.

# Task Step Types
ACR Tasks supports three step types:
- **[cmd](#cmd)** to run a container as a function, enabling parameters passed to the containers `[ENTRYPOINT]`. `cmd` supports  run parameters including ports, volumes and other familiar `docker run` parameters, enabling unit and functional testing with concurrent container execution. 
- **[build](#build)** containers using familiar syntax of `docker build`
- **[push](#push)** supports `docker push` of newly built or retagged images to a registry, including ACR, Docker hub and other private registries.

## cmd
`cmd` is the basic execution of a container. `cmd` follows the following format:
```yaml
version: 1.0-preview-1
steps:
    - [cmd]: [containerImage]:[tag (optional)] [cmdParameters to the image]
```
Using cmd, `az acr run -f ...` executes the referenced image as a function. 

- Hello World docker hub image
    
    The most basic hello-world example would be:
    ```sh
    az acr run -f hello-world.yaml https://github.com/AzureCR/acr-tasks-sample.git
    ```
    This runs a quick build of the hell-world.yaml file which references the [hello-world image on docker hub](https://hub.docker.com/_/hello-world/). 

    **hello-world.yaml**
    ```yaml
    version: 1.0-preview-1
    steps:
        cmd: hello-world
    ```

- echo hello world 

    The following **bash-echo.yaml** will instance the [docker hub bash](https://hub.docker.com/_/bash/) image, executing `echo hello world`

    ```sh
    az acr run -f bash-echo.yaml https://github.com/AzureCR/acr-tasks-sample.git
    ```

    **bash-echo.yaml**
    ```yaml
    version: 1.0-preview-1
    steps:
        - cmd: bash echo hello world
    ```


### `cmd` Versioning

Versioning of containers run within a `cmd` uses the version specific tags. 

- Versioned bash
    
    The following example executes the [bash:3.0](https://hub.docker.com/_/bash/) image:

    ```sh
    az acr run -f bash-echo-3.yaml https://github.com/AzureCR/acr-tasks-sample.git
    ```
    **bash-echo-3.yaml**
    ```yaml
    version: 1.0-preview-1
    steps:
        - cmd: bash:3.0 echo hello world
    ```


### Custom Images

When executing `cmd`, the image reference follows the standard convention for docker run. Images not prefaced with a registry are assumed to originate from docker.io. The above example could equally be represented as:
```yaml
version: 1.0-preview-1
steps:
    - cmd: docker.io/bash:3.0 echo hello world
```

By using docker run conventions, any image, in any registry, private or public may be referenced in `cmd`. Images referenced in the same registry ACR Task is executing will not require additional credentials. 

- Run the bash image from your ACR. 

    Create a `bash-echo.yaml` file locally.
    
    Replace [yourregistry] with the name of your registry. 

    ```yaml
    version: 1.0-preview-1
    steps:
        - cmd: [yourregistry].azurecr.io/bash:3.0 echo hello world
    ```
    Run the following from the same directory as `bash-echo-yaml`
    ```sh
    az acr run -f bash-echo.yaml .
    ```

- Generalize Registry References

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
- [retry: [int] (optional)](#retry)
- [startDelay: int (in seconds) (optional)](#startDelay)
- [timeout: int (in seconds) (optional)](#timeout)
- [when: [string, string, ...] (optional)](#when)
- [workingDirectory: string (optional)](#workingDirectory)

## build
`build` represents a multi-tenant secure means of running `docker build` as a first-class primitive. 

`build:` follows the following syntax:

```yaml
version: 1.0-preview-1
steps:
    - [build]: -t [imageName] [context]
      [property]: [value]
```

### `-t` | `--image` (optional)

Defines the fully qualified image:tag of the built image.

As images may be used for inner task validations, such as functional tests, not all images require `push` to a registry. However, to instance an image within a Task execution, the image does need a name to reference. 

Unlike `az acr build`, running ACR Tasks does not provide default push behavior. With ACR Tasks, the default scenario assumes the ability to build, validate, then push an image. See [push](#push) for how to optionally push built images. 

### `-f` | `--file` (optional)
References the Dockerfile passed to `docker build`. If not specified, the default Dockerfile will be searched within the root of the context. To specify an alternative Dockerfile, pass the filename, in reference to the context.

### context
The root directory passed to `docker build`. The root directory of each task is set to a shared [workingDirectory](#workingDirectory). This includes the root of the associated git cloned directory. 

- Building an image from the root

    To build a hello world image:
    ```sh
    az acr run -f build-hello-world.yaml https://github.com/AzureCR/acr-tasks-sample.git
    ```

    **build-hello-world.yaml**
    ```yaml
    version: 1.0-preview-1
    steps:
    - build: -t {{.Run.Registry}}/hello-world -f hello-world.dockerfile .
    ```

- Building an image form a sub directory

    ```yaml
    version: 1.0-preview-1
    steps:
    - build: -t {{.Run.Registry}}/hello-world -f hello-world.dockerfile ./subDirectory
    ```


### Build Properties
Supported build properties include:
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
Push newly built or re-tagged images to a specified registry.
```sh
az acr run -f build-push-hello-world.yaml https://github.com/AzureCR/acr-tasks-sample.git
```
**build-push-hello-world.yaml**
```yaml
version: 1.0-preview-1
steps:
  - build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} -f hello-world.dockerfile .
  - push: {{.Run.Registry}}/hello-world:{{.Run.ID}}
```

### push Properties
Supported push properties include:
- [env: [string, string, ...] (optional)](#env)
- [exitedWith: [int, int, ...] (optional)](#exitedWith)
- [exitedWithout: [int, int, ...] (optional)](#exitedWithout)
- [id: string (optional)](#id)
- [ignoreErrors: bool (optional)](#ignoreErrors)
- [retry: [int] (optional)](#retry)
- [startDelay: int (in seconds) (optional)](#startDelay)
- [timeout: int (in seconds) (optional)](#timeout)
- [when: [string, string, ...] (optional)](#when)

### Build, Push, Run hello-world

To build and run hello-world:
```sh
az acr run -f build-run-hello-world.yaml https://github.com/AzureCR/acr-tasks-sample.git
```

```yaml
version: 1.0-preview-1
steps:
  - build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} -f hello-world.dockerfile .
  - push: {{.Run.Registry}}/hello-world:{{.Run.ID}}
  - cmd: {{.Run.Registry}}/hello-world:{{.Run.ID}}
```


# Task Step Properties
Each step type supports a collection of relevant step properties. See [cmd](#cmd), [build](#build), [push](#push) step reference for which properties apply.

## detach:
`detach` determines whether or not the container should be detached when running.

## entryPoint:
`entryPoint` overrides the entry point of a step's container.

## env:
`env` is a list of strings in `key=val` format which define environment variables for a step.

## exitedWith:
`exitedWith` can be used to trigger a task when previous steps exited with one or more of the specified exit codes.

## exitedWithout:
`exitedWithout` can be used to trigger a task when previous steps exited without one or more of the specified exit codes.
[TODO: Example]

## id:
The `id` property is a unique identifier to reference the step throughout the task.
The id is also used as a DNS host name, when referencing images currently running. 

### id: Example
- Build two images, instancing a functional test image
    ```sh
    az acr run -f when-parallel-dependent.yaml https://github.com/AzureCR/acr-tasks-sample.git
    ```
    **when-parallel-dependent.yaml**
    ```yaml
    version: 1.0-preview-1
    steps:
        # build website and func-test images, concurrently
        - id: build-hello-world
          build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} .
          when: ["-"]
        - id: build-hello-world-test
          build: -t hello-world-test .
          when: ["-"]
        # run built images to be tested
        - id: hello-world
          cmd: {{.Run.Registry}}/hello-world:{{.Run.ID}}
          ports: 80:80
          when: ["build-hello-world"]
        - id: func-tests
          cmd: hello-world-func-test
          env: TEST_TARGET_URL=hello-world
          when: ["hello-world"]
    ```

## ignoreErrors:
If `ignoreErrors` is set to `true`, the step will be marked as complete regardless of whether or not an error occurred during its execution. Defaults to false.

## keep:
`keep` determines whether or not the step's container should be kept after execution.

## ports:
`ports` is a list of ports to publish to the host.

## retry:
The number of retry attempts to be made before failure

### retry example
```yaml
version: 1.0-preview-1
steps:
    - cmd: bash ping azurecr.io
        retry: 3
```

## retryDelay:
The number of seconds to pause after a failed attempt has been made before retrying to execute a step.

### retryDelay example

Ping a url 3 times, with a 2 second delay

```yaml
version: 1.0-preview-1
steps:
    - cmd: bash ping azurecr.io
        retry: 3
        retryDelay: 2
```

## startDelay:
`startDelay:` The number of seconds used to delay a step's execution. 

## timeout:
`timeout:` The maximum number of seconds for a step may execute before termination.

## when:
`when` is used to control a steps dependency on other steps. 
`when` supports two parameter values:

- **`when: ["-"]`** - indicates no dependency. A step with `when: ["-"]` will begin execution immediately.
- **`when: ["id1", "id2"]`** - indicates the step is dependent upon steps with `id: id1` and `id: id2`. 

If `when:` isn't provided, the step is dependent on the previous step in the yaml file.

### `when:` examples
- Sequential execution without declaring `"-"`
    ```sh
    az acr run -f when-sequential-default.yaml https://github.com/AzureCR/acr-tasks-sample.git
    ```
    **when-sequential-default.yaml**
    ```yaml
    version: 1.0-preview-1
    steps:
        - cmd: bash echo one
        - cmd: bash echo two
        - cmd: bash echo three
    ```
- Sequential execution, referencing step id's
    ```sh
    az acr run -f when-sequential-id.yaml https://github.com/AzureCR/acr-tasks-sample.git
    ```
    **when-sequential-id.yaml**
    ```yaml
    version: 1.0-preview-1
    steps:
        - id: step1
          cmd: bash echo one
        - id: step2
          cmd: bash echo two
          when: ["step1"]
        - id: step3
          cmd: bash echo three
          when: ["step2"]
    ```
- Parallel Builds
    ```sh
    az acr run -f when-parallel.yaml https://github.com/AzureCR/acr-tasks-sample.git
    ```
    **when-parallel-dependent.yaml**
    ```yaml
    version: 1.0-preview-1
    steps:
        # build website and func-test images, concurrently
        - id: build-hello-world
          build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} .
          when: ["-"]
        - id: build-hello-world-test
          build: -t hello-world-test .
          when: ["-"]
    ```

- Parallel Builds, with Dependent Testing
    ```sh
    az acr run -f when-parallel-dependent.yaml https://github.com/AzureCR/acr-tasks-sample.git
    ```
    **when-parallel-dependent.yaml**
    ```yaml
    version: 1.0-preview-1
    steps:
        # build website and func-test images, concurrently
        - id: build-hello-world
          build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} .
          when: ["-"]
        - id: build-hello-world-test
          build: -t hello-world-test .
          when: ["-"]
        # run built images to be tested
        - id: hello-world
          cmd: {{.Run.Registry}}/hello-world:{{.Run.ID}}
          ports: 80:80
          when: ["build-hello-world"]
        - id: func-tests
          cmd: hello-world-func-test
          env: TEST_TARGET_URL=hello-world
          when: ["hello-world"]
        # push hello-world if func-tests are successful  
        - push: {{.Run.Registry}}/hello-world:{{.Run.ID}}
          when: ["func-tests"]
    ```


## workingDirectory
`workingDirectory:` can be used to set a working directory when executing a step. By default, Azure Container Builder will produce a default root directory as the working directory. However, if your build has more than one step, you can share the artifacts created from previous steps.


# Run Properties

ACR Tasks supports a set of default properties. 

The following variables can be accessed using `{{.Run.VariableName}}`, where `VariableName` equals one of the following:

## Run&#46;ID
Each Run, through `az acr run`, or trigger based execution of tasks created through `az acr task create` have a unique ID. The ID represents the Run currently being executed. 


### Run&#46;ID Example
Typically used for a uniquely tagging an image:
```yaml
version: 1.0-preview-1
steps:
    - build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} .
```

## Run.Commit
The git-commit id of the underlying git repository. Commit should not be used as a unique identifier of image builds as images may be built based on a base image update, or manual trigger through `az acr run`.

### Run.Commit Example
```yaml
version: 1.0-preview-1
steps:
    - build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}}-{{Run.Commit}} .
```

## Run.Repository
[TODO:]
??Is this the git repository, or the ACR Repository??

## Run.Branch
The git branch of the underlying git repository

## Run.TriggeredBy
Task runs can be triggered in multiple ways:

- Manual: using `az acr run` or `az acr run`. 
- Git Commit: when triggered by a git commit
- Image Update: when triggered by a base image update.

## Run.Registry
The fully qualified login name of the registry. 
### Run.Registry Example
Typically used to generically reference the registry where the task is being run.
```yaml
version: 1.0-preview-1
steps:
    - build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} .
```

## Run.GitTag
[TODO:]

## Run.Date
The current date time the run began.

## Run.SharedContextDirectory
[TODO:]

> [!div class="nextstepaction"]
* TBD: