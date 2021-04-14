---
title: Connected registry error code reference
description: Details about error codes shown in the statusDetails property of a connected registry resource. For each error, possible solutions are listed.
ms.topic: article
ms.date: 04/13/2021
ms.author: jeburke
author: jaysterp
---

# Connected registry error code reference

This article helps you troubleshoot error codes you might encounter in the `StatusDetails` property of a connected registry.

## Connection State

The connection state of a connected registry indicates the current overall health status of the on-premises instance. The connection state is `Online` when the instance is healthy, `Offline` when the instance is not connected to internet, and `Unhealthy` when there is a critical error on the instance while it is online. When the connected registry resource has a connection state of `Unhealthy`, you may reference the `StatusDetails` property to view the corresponding error.

## Status Details Format

When your connected registry has a connection state of `Unhealthy` you can run the `az acr connected-registry show` command to view the statusDetails.

`StatusDetails` provides a list of errors, each with the following format:

```json
{
  "code": "Error code",
  "correlationId": "CorrelationId of the error on the on-premises connected registry instance",
  "description": "Description corresponding to this error",
  "timestamp": "Timestamp corresponding to this error",
  "type": "Component of the connected registry instance corresponding to the error"
}
```

Every time the connected registry instance syncs with the cloud, these status details are updated. When the connected registry no longer has status details listed, it is considered healthy and its connection state is transitioned from `Unhealthy` to `Online`. Once the connected registry is no longer connected to internet, its connection state will transition to `Offline`.

## Error Code Reference

This section lists the possible codes you may see in the `StatusDetails` property of a connected registry, which indicate critical errors. For each error, possible solutions are listed.

### DISK_PERMISSION_DENIED

The connected registry was unable to write any file to the disk because it did not have sufficient permissions.

*Potential solution:* Ensure that the host storage path used to run the connected registry container gives sufficient access to the container user. Update the permissions of the host system directory so that the user profile for your container has read, write, and execute access. By default, docker containers run as root. If the container is run as a non-root user, please ensure that user has the above permissions.

### DISK_STORAGE_FULL

The connected registry was unable to write files to the local host because there was no storage available on disk.

*Potential solution:* Connected registry container logs are integrated with docker. By default, docker does not set container log size limits. Over time, these logs can take up much of your host's storage capacity. If your disk is out of space then you can place limits on the container logs retained by docker.

#### Option 1: Place global log limits for all containers on the host

Create or update the docker daemon file `/etc/docker/daemon.json` to add logging limits accross all containers on this host. The following example sets the log driver to `json-file` and sets `max-size` and `max-file` properties in order to enable automatic log rotation. If the configured threshold is reached, docker will remove the oldest log file first in order to make space for new logs.

```json
{
    "log-driver": "json-file",
    "log-opts": {
        "max-size": "10m",
        "max-file": "3"
    }
}
```

Restart the container engine in order for the configuration to take effect.

#### Option 2: Place log limits only for the connected registry container only

You can also update the log level of the connected registry container only. Add the following flags to your `docker run` command:

`--log-driver json-file --log-opt max-size=10m --log-opt max-file=3`

Please reference how to set module-level log restrictions when running your connected registry on [IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/production-checklist?view=iotedge-2020-11#option-adjust-log-settings-for-each-container-module).

#### Update log verbosity on your connected registry

After making the above docker configuration changes to free up disk space, you can also update the connected registry resource in order to limit logs sent to docker. By default, connected registries are created with `Information` log level. To minimize the verbosity of the logs stored, set the log level to `Warning`, `Error`, or `None`. Using the Az CLI, run

`az acr connected-registry update -r MyRegistry -n MyConnectedRegistry --log-level Error`

The configuration will take effect on-premises during the next scheduled sync with the cloud.

### DISK_ERROR

This is the default error code when the connected registry is unable create, write, or delete a file on the local disk.
