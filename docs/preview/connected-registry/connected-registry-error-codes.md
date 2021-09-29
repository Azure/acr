---
title: Connected registry error code reference
description: Details about error codes shown in the statusDetails property of a connected registry resource. For each error, possible solutions are listed.
ms.topic: troubleshooting
ms.date: 09/29/2021
ms.author: jeburke
author: jaysterp
---

# Connected Registry Error Code Reference

This article helps you troubleshoot error codes you might encounter in the `StatusDetails` property of a connected registry.

## Connection State

The connection state of a connected registry indicates the current overall health status of the on-premises instance. The connection state is `Online` when the instance is healthy, `Offline` when the instance is not connected to internet, and `Unhealthy` when there is a critical error on the instance while it is online. When the connected registry resource has a connection state of `Unhealthy`, you may reference the `StatusDetails` property to view the corresponding error.

Use the [az acr connected-registry show][az-acr-connected-registry-show] command to view the current connection state of your connected registry.

```azurecli
az acr connected-registry show \
  --registry MyRegistry \
  --name MyConnectedRegistry \
  --output table
```

You should see a response as follows. Note that this connected registry has a connection state of `Unhealthy`.

```
NAME                   MODE      CONNECTION STATE    PARENT    LOGIN SERVER    LAST SYNC (UTC)            SYNC SCHEDULE    SYNC WINDOW
---------------------  --------  ------------------  --------  --------------  -------------------------  ---------------  -------------
MyConnectedRegistry    ReadOnly  Unhealthy                                     2021-09-29T12:59:00+00:00  * * * * *
```

## Status Details Format

When your connected registry has a connection state of `Unhealthy` you can run the [az acr connected-registry show][az-acr-connected-registry-show] command to view the list of status details.

```azurecli
az acr connected-registry show \
  --registry MyRegistry \
  --name MyConnectedRegistry
  --query statusDetails
```

The `StatusDetails` property provides a list of error objects, each with the following format:

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

# Error Codes

This section lists the possible codes you may see in the `StatusDetails` property of a connected registry, which indicate critical errors. For each error, possible solutions are listed. You can view the status details of your connected registry by running the [az acr connected-registry show][az-acr-connected-registry-show] command.

```azurecli
az acr connected-registry show \
  --registry MyRegistry \
  --name MyConnectedRegistry
  --query statusDetails
```

## DiskError

This is the default error code when the connected registry is unable to create, write, or delete a file on the local disk. There are a few scenarios that may cause a `DiskError` code. Please reference below for possible scenarios and remediations. 

### Insufficient Permissions

Sample status detail:

```json
[
    {
      "code": "DiskError",
      "correlationId": "73a46395-b89b-49c7-5621-d54e8b1574b5",
      "description": "Access to the path '/var/acr/data/registry/dummy.txt' is denied.",
      "timestamp": "2021-09-16T01:17:45.394512+00:00",
      "type": "Disk"
    }
]
```

This status `description` indicates that the connected registry was unable to write any file to the disk because it did not have sufficient permissions.

*Potential solution:* Ensure that the host storage path used to run the connected registry container gives sufficient access to the container user. In the sample above, this path is `/var/acr/data/registry`. Update the permissions of the host system directory so that the user profile for your container has read, write, and execute access. By default, docker containers run as root. If the container is run as a non-root user, please ensure that user has the above permissions.

### No Storage Available On Disk

Sample status detail:

```json
[
    {
      "code": "DiskError",
      "correlationId": "73a46395-b89b-49c7-5621-d54e8b1574b5",
      "description": "There is not enough space on the disk.",
      "timestamp": "2021-09-16T01:17:45.394512+00:00",
      "type": "Disk"
    }
]
```

This status `description` indicates that the connected registry was unable to write any file to the disk because it did not have sufficient permissions.

*Potential solution:* Connected registry container logs are integrated with docker. By default, docker does not set container log size limits. Over time, these logs can take up much of your host's storage capacity. If your disk is out of space then you can place limits on the container logs retained by docker. See the following options for limiting storage space used by connected registry logs.

#### Option 1: Place global log limits for all containers on the host

If running the connected registry on docker, create or update the docker daemon file `/etc/docker/daemon.json` to add logging limits accross all containers on this host. The following example sets the log driver to `json-file` and sets `max-size` and `max-file` properties in order to enable automatic log rotation. If the configured threshold is reached, docker will remove the oldest log file first in order to make space for new logs.

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

#### Option 2: Place log limits only for the connected registry container

You can also update the log level of the connected registry container only. Add the following flags to your `docker run` command:

`--log-driver json-file --log-opt max-size=10m --log-opt max-file=3`

Please reference how to set module-level log restrictions when running your connected registry on [IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/production-checklist?view=iotedge-2020-11#option-adjust-log-settings-for-each-container-module).

#### Option 3: Update log verbosity on your connected registry

After making the above docker configuration changes to free up disk space, you can also update the connected registry resource in order to limit logs sent to docker. By default, connected registries are created with `Information` log level. To minimize the verbosity of the logs stored, set the log level to `Warning`, `Error`, or `None`. Use the connected registry [az acr connected-registry update][az-acr-connected-registry-update] command to update the log level.

`az acr connected-registry update -r MyRegistry -n MyConnectedRegistry --log-level Error`

```azurecli
az acr connected-registry update \
  --registry MyRegistry \
  --name MyConnectedRegistry \
  --log-level Error
```

The configuration will take effect on-premises during the next scheduled sync with the cloud.

<!-- LINKS - internal -->
[az-acr-connected-registry-show]: https://docs.microsoft.com/cli/azure/acr/connected-registry?view=azure-cli-latest#az_acr_connected_registry_show
[az-acr-connected-registry-update]: https://docs.microsoft.com/cli/azure/acr/connected-registry?view=azure-cli-latest#az_acr_connected_registry_update
