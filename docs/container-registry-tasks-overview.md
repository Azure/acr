---
title: Automate OS and Framework Patching with Azure Container Registry Tasks
description: An introduction to ACR Tasks, a suite of features in Azure Container Registry that provides secure, automated container image build, test and patching in the cloud.
services: container-registry
author: stevelas
manager: balans

ms.service: container-registry
ms.topic: article
ms.date: 08/30/2018
ms.author: stevelas
---

# Automate OS & Framework Patching with ACR Tasks

ACR Tasks provide a container centric compute primitive, focused on building and patching container workloads.

ACR Tasks are a series of steps representing execution of one or more containers, using the container as the execution environment. ACR Tasks are defined with a `.yaml` file, identifying the steps and the dependencies each steps has upon another. 
ACR Tasks can be as simple as building a single image:
```yaml
version: 1.0.0
steps:
  - build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} .
  - push: {{.Run.Registry}}/hello-world:{{.Run.ID}}
```

To more complex build, test, helm package, helm deploy scenarios:
```yaml
version: 1.0.0
steps:
  - id: build-web
    build: -t {{.Run.Registry}}/hello-world:{{.Run.ID}} .
    when: ["-"]
  - id: build-tests
    build -t {{.Run.Registry}}/hello-world-tests ./funcTests
    when: ["-"]
  - id: push
    push: {{.Run.Registry}}/helloworld:{{.Run.ID}}
    when: ["build", "build-tests"]
  - id: hello-world-web
    cmd: {{.Run.Registry}}/helloworld:{{.Run.ID}} 
  - id: funcTests
    cmd: {{.Run.Registry}}/helloworld:{{.Run.ID}} 
    env: host=helloworld:80
    when: ["hello-world-web"]
  - cmd: {{.Run.Registry}}/functions/helm package --app-version {{.Run.ID}} -d ./helm ./helm/helloworld/
    when: ["funcTests"]
  - cmd: {{.Run.Registry}}/functions/helm upgrade helloworld ./helm/helloworld/ --reuse-values --set helloworld.image={{.Run.Registry}}/helloworld:{{.Run.ID}}
```

Through ACR Tasks, developers can:

- **[build](container-registry-task-ref-build.md)** containers using familiar syntax of `docker build`
- **[cmd](container-registry-task-ref-cmd.md)** to run a container as a function, enabling parameters passed to the container [ENTRYPOINT]. `cmd` supports  run parameters including ports, volumes and other familiar `docker run` parameters, enabling unit and functional testing with concurrent container execution. 
- **[push](container-registry-task-ref-push.md)** newly built images to a registry, including ACR, Docker hub and other private registries.

## ACR Task Common Scenarios

The most common scenarios include:

- Building, tagging and pushing 1 or more container images; in series or in parallel.
- Running and capturing unit test and code coverage results.
- Running and capturing functional tests. ACR Tasks supports running multiple container,s executing a series of requests between them.
- Task based execution, including pre/post steps of a container build. 
- Deploying 1 or more containers with your favorite deployment engine to your target environment. 

## ACR Tasks Support the 3 Primary Phases of Development

ACR Tasks highlight 3 phases of container life cycle management. 
- **Inner Loop Development** - Before developers git-commit their code, they can test their container builds and tasks with `az acr task run .`
- **Team based commits** - Whether a team of 1, or 100, as git commits are made, tasks can be triggered for execution. See [az acr task create](container-registry-task-create.md) for establishing trigger based execution.
- **Post development, OS & Framework Patching** - When developing and deploying containers, the means to patch a container involves rebuilding the image, testing and deploying the newly built and tested images. ACR Tasks support [base image update triggers](container-registry-task-create.md#BaseImageTriggers), enabling a task to run as the runtime or buildtime dependent images are updated. 


## ACR Tasks Support Simple to Complex Workloads, Integrating with CI/CD Solutions

Many developers may find ACR Tasks meets their needs. As the complexity increases, or users which to integrate into their existing CI/CD solutions, ACR Tasks can be integrated with CI/CD pipelines getting the benefits of fast, cloud native container execution, with the robust capabilities of other CI/CD solutions. 

### Scoping and Positioning ACR Tasks With Other Azure Container Primitives

As containers continue to become the [common unit of custom and ISV code deployment](https://blogs.msdn.microsoft.com/stevelasker/2016/05/26/docker-containers-as-the-new-binaries-of-deployment/), Azure container hosting continues to expand. ACR Tasks are intended to fill a gap between ACI, AKS, Batch, App Services and other Azure Services. ACR Tasks are focused on short lived execution, with multi-tenant isolation capabilities. Customers building and testing their containers should have performance similar to local builds. This includes task execution queuing, scheduling, streaming of logs. 
> Note: performance will continue to increase as other features come online.

However, we don't know what we don't know, and we seek your feedback:

## ACR Tasks Preview Feedback

ACR Tasks evolved from the container life cycle management efforts, focusing on [OS & Framework patching of containers](https://blogs.msdn.microsoft.com/stevelasker/2017/12/20/os-framework-patching-with-docker-containers-paradigm-shift/). For containers to evolve past the complexity of patching and testing virtual machines, ACR Build required the ability to run test containers. As we explored various options, we focused on the simplicity of running a container, passing in arguments and letting the developer choose what and how they wish to run their tests. The ability to run containers, for short lived bursts, at cloud scale is core to ACR Tasks. This primitive has exposed other possibilities and we seek your feedback. 

- [Roadmap](https://aka.ms/acr/roadmap) - for visibility into our planned work
- [UserVoice](https://aka.ms/acr/uservoice) - to vote for existing requests, or create a new request
- [Feedback](https://aka.ms/acr/feedback) - to provide feedback, engage in discussion with the community
- [Issues](https://aka.ms/acr/issues) - to view existing bugs and issues, logging new ones

## Next steps

To learn more about ACR Tasks, drill into the following topics:

> [!div class="nextstepaction"]
* [ACR Task Step Reference](container-registry-ref-acr-steps.md)


