> [!NOTE]
> Please visit [aka.ms/acr/artifact-streaming](https://aka.ms/acr/artifact-streaming).

---
type: post
title: "Overview"
excerpt: "project teleport"
tags: [developers, teleport]
date: 2019-11-01 17:00:00
author: Steve Lasker
---

# Azure Container Registry Adds Teleportation

![](https://stevelaskerblog.files.wordpress.com/2019/10/image-8.png?w=132)


Instancing a custom environment within seconds is one of the many wonders of running containers. Having to wait for the image and its layers to download & decompress the first time is the _current_ price of admission.

> Project Teleport removes the cost of download and decompression by SMB mounting pre-expanded layers from the Azure Container Registry to Teleport enabled Azure container hosts.

## Teleportation Performance

The following table represents initial performance metrics across different image sizes. The amount of time to teleport an image has less to do with the size of the image, but rather the number of layers that must be mounted. This is an area of performance we’ll continue to focus upon.

![](https://stevelaskerblog.files.wordpress.com/2019/10/teleportmetricslayers.png?w=1024)


> Our opportunistic goal for Project Teleport is 90% of locally cached images. We’re not considering teleportation of organic material, as 90% would not be _quite_ good enough. Being able to pull any custom image, to any serverless host, at 90% of the startup time, seems pretty good. Especially considering it’s a 100% unattainable goal of having every custom image on every serverless host.
> 
> <cite>Steve Lasker – Program Manager – Azure Container Registries</cite>

## How Project Teleport Optimizes Registry Operations

While Docker didn’t invent containers, they did provide a highly productive end to end experience for building, pushing, discovering, pulling and instancing containers. Container registries are one of the innovations that provide content addressable objects through a collection of layers.

![](https://stevelaskerblog.files.wordpress.com/2019/10/dockerimagepull.png?w=1024)

The underlying container flow today involves:

1.  pulling an image, which calls a REST endpoint, returning a collection of layer IDs
2.  comparing the local cache, determining the delta of layers that must be retrieved
3.  requesting secured URLs for each layer ID
4.  pulling each layer
5.  decompressing each layer
6.  instancing the container when all the layers are available

This flow works well for internet protocols where the network is comparatively unreliable and slower than an intra-datacenter network. For a reasonably sized image, it’s faster to serve compressed blobs, decompressing on the client, than waiting for larger payloads to fight with YouTube, Netflix and a million other packets traveling across the wild internet.

When running within a controlled datacenter, the network is reliable and fast, while CPU, disk speed and memory become the bottleneck for pulling complete layers and decompressing them before usage.

When using dedicated hosts, such as VMs provisioned for Kubernetes, pulling the first image is painful, but subsequent image pulls benefit from the pre-cached layers. As clouds move to “serverless” environments, where the hosts are dynamically allocated, each container run is a new environment. Cloud providers pre-cache the most common base layers, but the hit ratio varies across each service and time, as newer versions are continually released. This creates an inconsistent experience, detracting from the value of serverless.

## Highly Factored Registry Protocol

The designers of the [distribution-spec](https://github.com/opencontainers/distribution-spec/) and [image-spec](https://github.com/opencontainers/image-spec) created a highly factored protocol that enables cloud providers to adhere to a public spec, with the flexibility to implement cloud specific storage and authentication solutions. Project Teleport takes advantage of this factoring by adhering to the public API that container developers are accustomed to, while providing cloud specific optimizations.

[](https://stevelaskerblog.files.wordpress.com/2019/10/telportimagepull.png?w=1024)

Project Teleport assumes the image pull runs within an optimized environment. The underlying Teleport flow is slightly, but very impactfully different, involving:

1.  pulling an image, which involves a REST endpoint that returns the collection of layer IDs
2.  comparing the local cache, determining the delta of layers that must be retrieved
3.  **_requesting [Azure Premium File](https://azure.microsoft.com/en-us/blog/announcing-the-general-availability-of-azure-premium-files/) mount points for each layer ID_**
4.  **_[SMB](https://en.wikipedia.org/wiki/Server_Message_Block) mounting each layer as pre-expanded content_**
5.  instancing the container when all the layers are available

The benefits of Project Teleport include:

*   when using the SMB protocol, only the content read by the container is pulled across the network, speeding container start time
*   no decompression in the run flow, removing the additional CPU, local disk speed and memory bottlenecks
*   overall reduced network traffic, as only the subset of an image that’s utilized is pulled across the network
*   the ability to leverage local image cache information as the teleported mounts intermix with the local cache

## Orca, a Teleport Client for Azure

Project Teleport is a registry transport protocol, enabling container layers to be teleported from the registry directly to a container host. Normally, you would issue docker run commands to pull and run an image. However, we need a means to plugin the teleport protocol to the container host. Project Teleport takes advantage of the [containerd snapshot plugin](https://github.com/containerd/containerd#snapshot-plugins). As containerd and the docker client evolve, we’ll simply plugin Project Teleport to a new docker client. Until that time, we provide an orca client, for a subset of docker functionality, focusing on the running of container images. For instance, container building is not yet supported.

| | | 
|-----------------|-----------------------|
| ![](https://stevelaskerblog.files.wordpress.com/2019/10/image-2.png?w=363)| ![](https://stevelaskerblog.files.wordpress.com/2019/10/image-3.png?w=245) | 
| Orca represents the amazing Orca species of whales, roaming the Seattle Puget Sound.|  Our own Brendan Burns also has a sailboat, appropriately named Orca.|


## Previewing Teleportation with ACR Tasks

While our goal is to enable Project Teleport on all Azure Services, today we are previewing Teleport with [ACR Tasks](https://aka.ms/acr/tasks). ACR Tasks provides the ability build and run container images in a highly optimized and securely isolated environment. The initial Project Teleport preview focuses on running Linux images.

Because ACR Tasks is a focused environment, we can preview teleportation with customer provided images without having to support a large surface area. Based on your feedback and the evolution of containerd, we’ll know when we can expand usage to other Azure services

## Running Containers With the Orca Client, Using the Teleport Transport

The following two commands demonstrate running ACR Tasks with and without Project Teleport.

**ACR Run:**

`az acr run -r demo42 --cmd "demo42.azurecr.io/batchprocessor:1" /dev/null`  
The above command executes an [ACR Task](https://aka.ms/acr/task), on the `demo42` registry. The `--cmd` parameter runs the `batchprocessor` image. Like `docker run`, `acr task run` takes a positional argument that represents the context. Since we’re not passing a context, we just pass `/dev/null`

**Teleporting the batchprocessor image:**

`az acr run -r demo42 --cmd "**orca run** demo42.azurecr.io/batchprocessor:1" /dev/null`  
The above command instructs [ACR Tasks](https://aka.ms/acr/task) to use the orca client to run the `batchprocessor` image. Over time, the `--cmd` parameter will directly support Teleport enabled images, removing the need to specify `orca run`.

## Under the Hood of an Image Teleporter

[](https://stevelaskerblog.files.wordpress.com/2019/10/dockerorca.png?w=1024)

Within ACR, we’ve expanded support from compressed blobs, using [Azure Blob Storage](https://azure.microsoft.com/services/storage/blobs/), to [Azure Premium Files](https://azure.microsoft.com/en-us/blog/announcing-the-general-availability-of-azure-premium-files/), storing expanded layers. Each expanded layer is persisted as a [virtual hard disk (.vhd)](https://en.wikipedia.org/wiki/VHD_(file_format)) which are supported by Linux and Windows clients.

To support standard docker clients, or any client capable of pushing a container image, ACR accepts the incoming image and checks to see if the target repository supports teleportation. If the repository is teleport enabled, an **ACR expansion service** creates a decompressed .vhd for each layer. By storing each layer as a .vhd, ACR can continue to maintain de-duping of common layers across multiple images, while maintaining repository based RBAC.

When a request is made to pull an image, the orca client provides header information stating the region and whether it’s teleport enabled. If the registry is in the same region, teleport SMB mount points are returned. If the client is in a different region, a fallback to compressed blob URLs are returned.

> The SMB Teleporter depends on intra-datacenter networks, limiting short range teleportation. To enable a [best practice of having images network-close to the container host](https://stevelasker.blog/2018/11/14/choosing-a-docker-container-registry/), future releases will support multi-region Teleportation through an [ACR Geo-replication](https://aka.ms/acr/geo-replication) translocator.

[](https://stevelaskerblog.files.wordpress.com/2019/10/orcadocker.png?w=1024)

In future releases we plan to enable ACR Task build support, teleporting base images and writing new image layers directly to the registry. As the image build completes, ACR will compress the layers into traditional blobs, enabling standard docker clients.

![](https://stevelaskerblog.files.wordpress.com/2019/10/orcaorca.png?w=1024)

When paired with [ACR Task buildx caching](https://github.com/Azure/acr/blob/master/docs/Tasks/buildx/README.md), dramatic improvements from code-commit to deploy performance can be realized.

## The Future of Container Teleportation

The future of Project Teleport is broken into the following categories:

*   Incorporating user feedback
*   Improved mounting performance
*   Supporting all Azure services using containers
*   Windows containers
*   Building images, teleporting the base layers and writing expanded layers directly to the registry
*   Geo-replication translocation

Thankfully, the teleport project is split across multiple teams, enabling parallelization.

## Teleporting Images Across All Azure Container Hosts

Project Teleport is designed to support all container hosts, including Linux & Windows, and all Azure services. This includes AKS, ACI, Virtual Kubelet, Machine Learning, ACR Tasks and the golden serverless scenario, Azure Functions.

## Teleporting Serverless Functions

When we think about serverless functions, the ability to instantly run some custom set of code becomes the holy grail. The service must scale from 0 to infinity (_and beyond_), while only charging for the actual usage. The reference to **_instant_** and **_custom code_** is the challenge. Today, serverless platforms utilize containers to host known environments for specific language runtimes. To achieve specific language runtimes, services mount user code into a pre-allocated pool of container instances. Pulling custom images is just too slow. With Project Teleport, we can now expand the environments and the languages you prefer, bringing whatever custom images you desire in near instant time.

## How Can You Teleport Your Containers?

The customer feedback we get with ACR Tasks will help us improve teleportation across all Azure service hosts. We’ve been working on Teleportation since early 2018, so we’re obviously excited to hear what you think, and learn how we need to complete the scenarios. After the first round of a private preview feedback, we’ll open a public preview.

*   To Help us test teleportation of your images – [sign up here](https://aka.ms/teleport/signup) for the private preview
*   Are you just as excited with container scenarios, building teleporters and other [ACR roadmap capabilities](https://aka.ms/acr/roadmap)? Apply here for [ACR Jobs](https://aka.ms/acr/jobs)

---
Steve Lasker  
