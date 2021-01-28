---
title: Troubleshoot issues with connected registry
description: Symptoms, causes, and resolution of common problems when setting up, configuring, and deploying connected registries
ms.topic: article
ms.date: 01/27/2021
ms.author: memladen
author: toddysm
---

# Troubleshoot issues with connected registry

This article helps you troubleshoot problems you might encounter when setting up, configuring, and deploying a connected registry.

## Symptoms

May include one or more of the following:

* Unable to push or pull images to or from the connected registry. Client error is `Error response from daemon: Get https://<connected-registry-login-server-ip-or-dns>/v2/: http: server gave HTTP response to HTTPS client`

## Causes

* The connected registry is configured for HTTP access only - [solution](#configure-docker-daemon-to-access-insecure-registry)

## Potential solutions

### Configure Docker daemon to access insecure registry

The access the connected registry via HTTP, you must configure the client Docker daemon to allow access to insecure registries. The steps are described in [Test an insecure registry](https://docs.docker.com/registry/insecure/) article on Docker's web site.
