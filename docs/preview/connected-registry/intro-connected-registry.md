---
title: What is connected registry
description: Overview of the connected registry feature of Azure container registry introducing the main concepts
author: toddysm
ms.author: memladen
ms.service: container-registry
ms.topic: overview
ms.date: 01/11/2021
---

# What is connected registry? 

In this article, you learn about the connected registry feature of Azure container registry. A connected registry is an on-premise replica that synchronizes container images and other OCI artifacts with your cloud-based Azure container registry. Use a connected registry to help speed up access to registry artifacts on-premise and build advanced scenarios using [nested IoT Edge](https://docs.microsoft.com/azure/iot-edge/tutorial-nested-iot-edge), [Azure Stack](https://docs.microsoft.com/azure-stack), or [Azure Arc enabled Kubernetes](https://docs.microsoft.com/azure/azure-arc/kubernetes/overview).

## Scenarios
A cloud-based Azure container registry provides features including geo-replication, integrated security, Azure-managed storage, and integration with Azure development and deployment pipelines. At the same time, customers are extending their cloud investments to their on-premises and field solutions.

To run with the required performance and reliability in on-premises or remote environments, container workloads need container images and related artifacts to be available nearby. The connected registry provides a performant on-premises registry solution that regularly synchronizes content with a cloud-based Azure container registry.

Scenarios for a connected registry include:

* Connected factories
* Point-of-sale retail locations
* Shipping, oil-drilling, mining, and other occasionally connected environments

## How the connected registry works?

The following picture shows some of the deployment models for the connected registry.

![Connected Registry Overview](media/connected-registry/connected-registry-overview.svg)

* **Deployment** - Each connected registry is a resource you manage using a cloud-based Azure container registry. The top parent in the connected registry hierarchy is an Azure container registry in any of the Azure clouds or in a private deployment of [Azure Stack Hub](https://docs.microsoft.com/azure-stack/operator/azure-stack-overview).

    You use Azure tools to install the connected registry on a server or device on your premises. It can be deployed on any environment that supports container workloads on-premises including [Azure IoT Edge](https://docs.microsoft.com/azure/iot-edge/tutorial-nested-iot-edge), [Azure Stack Hub](https://docs.microsoft.com/azure-stack/operator/azure-stack-overview), and [Azure Arc enabled Kubernetes](https://docs.microsoft.com/azure/azure-arc/kubernetes/overview).

* **Content synchronization** - The connected registry regularly accesses the cloud registry to synchronize container images and OCI artifacts. 

    It can also be configured to synchronize just a subset of the repositories from the cloud registry or to synchronize only during certain intervals to reduce traffic between the cloud and the premises.

* **Modes** - The connected registry can work in one of two modes:

    - The default *registry* mode allows clients to pull as well as push artifacts to the connected registry. Artifacts that are pushed to the connected registry will be synchronized with the cloud registry. The *registry* mode is useful when a local development is in place. The images are pushed to the local connected registry and from there synchronized to the cloud.
    - When the connected registry is in _mirror_ mode, clients may only pull artifacts. This configuration is used for nested IoT Edge scenarios or when the client has limited capabilities such as an ARM device. The *mirror* mode is useful for all other scenarios where clients need to pull a container image to operate.

* **Registry hierarchy** - Each connected registry must be connected to a parent. The top parent is the cloud registry. For hierarchical scenarios, you can nest connected registries in *registry* or *mirror* mode. The parent connected to the cloud registry can operate in both *registry* or *mirror* mode. Children registries must be compatible with their parent capabilities: thus, both *registry* and *mirror* can be children of a connected registry operating in *registry* mode, but only *mirror* can be a child of a connected registry operating in *mirror* mode.  

## Client access

On-premises clients use standard tools such as the Docker CLI to push or pull content from a connected registry. To manage client access, you create Azure container registry [tokens][repository-scoped-permissions] for access to each connected registry. You can scope the client tokens for pull or push access to one or more repositories in the registry.

Each connected registry also needs to regularly communicate with its parent registry. For this purpose, the registry is issued a synchronization token (*sync token*) by the cloud registry. This token is used to authenticate with its parent registry for synchronization and management operations.

For more information about authentication and authorization for connected registries, see [Manage access to a connected registry][overview-connected-registry-access].

## Next steps

In this overview, you learned about the connected registry and some basic concepts. Continue to the one of the following articles to learn about specific scenarios where connected registry can be utilized.

> [!div class="nextstepaction"]
> [Overview: Connected registry access][overview-connected-registry-access]
> 
> [!div class="nextstepaction"]
> [Overview: Connected registry and IoT Edge][overview-connected-registry-and-iot-edge]

<!-- LINKS - internal -->
[overview-connected-registry-access]:overview-connected-registry-access.md
[overview-connected-registry-and-iot-edge]:overview-connected-registry-and-iot-edge.md
[repository-scoped-permissions]:[https://docs.microsoft.com/azure/container-registry/container-registry-repository-scoped-permissions]
