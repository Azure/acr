---
title: Quickstart - Secure a container registry with the Quarantine Policy
description: ACR supports securing a registry to only allow pulling images that have been scanned and approved by a vulnerability scanning solution. This quickstart will describe how to configure ACR to be secured by default.
services: container-registry
author: stevelas
manager: balans

ms.service: container-registry
ms.topic: article
ms.date: 04/12/2018
ms.author: stevelas
---

# Use Azure Container Registry Quarantine Policy to Secure a Registry From Unscanned Images

**The Quarantine Pattern** is a pattern by which all newly pushed images and tags, pushed to a registry are placed into a quarantined state. Only after the image:tag is scanned, and approved will it be available for pull.  

In this quickstart you enable Quarantine, configure a vulnerability scanning solution, then push a sample good and bad image.

> IMPORTANT: Quarantine is in currently in private-preview. 

## Get Access to ACR Quarantine

* **Access**: While ACR Quarantine is in preview, you must first request access at https://aka.ms/acr/preview/signup

## Create an Azure Container Registry
```sh
ACR_NAME=mycontainerregistry # Registry name - must be *unique* within Azure
RES_GROUP=$ACR_NAME # Resource Group name
REGISTRY_NAME=${ACR_NAME}.azurecr.io/

az group create -g $RES_GROUP -l eastus
az acr create -g $RES_GROUP -n $ACR_NAME --sku Basic
```

## Enable Quarantine Policy

> INFORMATION: While ACR Quarantine is in private-preview, please signup, requesting your registry to be quarantine enabled.

## Configure Vulnerability Scanning

ACR has partnered with [Aqua](https://www.aquasec.com/) and [Twistlock](https://www.twistlock.com/)

* [Configuring Aqua for the Quarantine Pattern](https://www.aquasec.com)
* [Configuring Twistlock for the Quarantine Pattern](https://docs.twistlock.com)

## Build a Sample Good Image

1. To demonstrate successful and failed image scans, create the following dockerfile. 
    ```sh
    FROM alpine:latest
    CMD ["/bin/sh"]
    ```
    
1.  Build the image:

    Based on the movie Outbreak, we'll use the name of the book Hotzone for our image name
    ```sh
    docker build -t ${REGISTRY_NAME}hotzone:good .
    ```

1.  Push the image to the registry
    ```sh
    docker push ${REGISTRY_NAME}hotzone:good
    ```

1. Attempt to pull the image
    ```sh
    docker pull ${REGISTRY_NAME}hotzone:good
        Error response from daemon: manifest for quarantinetest.azurecr.io/hotzone:good not found
    ```
    As the image is under quarantine, the pull fails

1.  List the images in the registry
    ```sh
    az acr repository show-tags --repository hotzone -n $ACR_NAME -o table
    Tag    State          Timestamp             Digest
    ----   -------------- --------------------  ------------------------ 
    good   Quarantine     2018-04-02T17:46:19Z  sha256:d022c2d4db054f926
    ```
    The registry reflects the tag :good, however the state is listed as **quarantine**, which restricts pulls

1.  List the images, after the scan has completed

    After a period of time, the image should complete it's scan and return **ScanSucceeded** state
    ```sh
    az acr repository show-tags --repository hotzone -n $ACR_NAME -o table
    Tag    State          Timestamp             Digest
    ----   -------------- --------------------  ------------------------ 
    good   ScanSucceeded  2018-04-02T17:48:42Z  sha256:d022c2d4db054f926
    ```

1.  Pull the scanned image
    ```sh
    docker pull ${REGISTRY_NAME}hotzone:good
    ...
    c96abd32e00c: Pull complete 
    Digest: sha256:d022c2d4db054f926
    Status: Downloaded newer image for quarantinetest2.azurecr-test.io/helloworld:v4
    ```
    
    Once the image scan successfully completes, the image can be pulled 


## View the Image in the Vulnerability Scanning Solution

* [Viewing scan results in Aqua](https://www.aquasec.com)
* [Viewing scan results in Twistlock](https://docs.twistlock.com)

## Build a Sample Bad Image

1. To demonstrate successful and failed image scans, create the following dockerfile. 
    ```sh
    FROM alpine:latest
    RUN apk update
    RUN apk add --no-cache openssh-client wget drill curl netcat-openbsd mtr clamav nmap openssl nano goaccess fping tcpdump socat iftop strace
    CMD ["/bin/sh"]
    ```

1.  Build the image:

    Based on the movie Outbreak, we'll use the name of the book Hotzone for our image name
    ```sh
    docker build -t ${REGISTRY_NAME}hotzone:bad .
    ```

1.  Push the image to the registry
    ```sh
    docker push ${REGISTRY_NAME}hotzone:bad
    ```

1.  List the images in the registry
    Rather than attempt to pull the image, we know will fail, view the image in the cli
    ```sh
    az acr repository show-tags --repository hotzone -n $ACR_NAME -o table
    Tag    State          Timestamp             Digest
    ----   -------------- --------------------  ------------------------ 
    good   ScanSucceeded  2018-04-02T17:48:42Z  sha256:d022c2d4db054f926
    bad    Quarantine     2018-04-02T28:32:42Z  sha256:efao222012da021fa
    ```
## View the Image in the Vulnerability Scanning Solution

* [Viewing scan results in Aqua](https://www.aquasec.com)
* [Viewing scan results in Twistlock](https://docs.twistlock.com)

## Clean up resources

To remove all resources you've created in this quickstart, including the container, container registry, key vault, and service principal, issue the following commands:

```sh
az group delete -g $RES_GROUP
```

## Next steps


### Feedback

While you're testing ACR Build, the team welcomes any and all feedback in the **#build** channel on Slack at [azurecr.slack.com](https://azurecr.slack.com).
