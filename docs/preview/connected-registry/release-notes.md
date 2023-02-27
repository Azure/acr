# Release Notes

Release notes for the Azure Container Registry connected registry runtime image. The image is published at `mcr.microsoft.com/acr/connected-registry:<tag>`.

## 0.9.0
February 23, 2023

* Update runtime image to .NET 6

tags: `0.9.0`, `0.9.0-linux-amd64`, `0.9.0-linux-arm32v7`, `0.9.0-linux-arm64v8`

## 0.8.0
July 29, 2022

* Bug fix for memory leak on connected registry instance.
* Bug fix to include Docker api version header in proxied responses.

tags: `0.8.0`, `0.8.0-linux-amd64`, `0.8.0-linux-arm32v7`, `0.8.0-linux-arm64v8`

## 0.7.0
January 21, 2022

* Bug fix for auth issue during the activation caused by mixed-case connected registry name.
* Increase the per request http client timeout used during sync from 5s to 10s. Useful for slow network scenarios.
* Retry gateway API requests during sync in case of timeout. Useful for slow network scenarios.

tags: `0.7.0`, `0.7.0-linux-amd64`, `0.7.0-linux-arm32v7`, `0.7.0-linux-arm64v8`

## 0.6.0
November 16, 2021

* Enable artifact push/delete notifications from the connected registry to the parent ACR. 
* Bug fix to ensure only once instance of `PartitionMessageFeed` is running. This was causing incorrect message sequence numbers during notification to parent.

tags: `0.6.0`, `0.6.0-linux-amd64`, `0.6.0-linux-arm32v7`, `0.6.0-linux-arm64v8`

## 0.5.0
October 28, 2021

* Support for `ReadWrite` and `ReadOnly` connected registry mode types.
* Support sync of OCI artifacts.
* Bug fix for syncing 0 byte layer.
* Bug fix where connection string could not be parsed if it started with the '=' char.

tags: `0.5.0`, `0.5.0-linux-amd64`, `0.5.0-linux-arm32v7`, `0.5.0-linux-arm64v8`

## 0.3.0
Jun 9, 2021

* Support connected registry recovery in case of a missed sync iteration.
* Support the `ACR_PARENT_GATEWAY_ENDPOINT` environment variable.
* If synced repositories are removed from the cloud connected registry settings, only clean from the local store if the connected registry is in `Mirror` mode.
* Log bearer authentication challenges as `Debug` level.
* Bug fix for continuation token issue when fetching tags.
* Bug fix to avoid duplicate configuration event processing.
* Bug fix to merge the allowed during authentication to the connected registry. This is required when using containerd to authenticate to the connected registry.

tags: `0.3.0`, `0.3.0-linux-amd64`, `0.3.0-linux-arm32v7`, `0.3.0-linux-arm64v8`

## 0.2.0
March 30, 2021

* Support hierarchical deployment of connected registries.
* Support connection string configuration during connected registry installation.
* Make `ACR_REGISTRY_LOGIN_SERVER` an optional environment variable.
* Bug fix to ensure that a scheduled sync iteration is cancelled after surpassing the sync window.

tags: `0.2.0`, `0.2.0-linux-amd64`, `0.2.0-linux-arm32v7`, `0.2.0-linux-arm64v8`

## 0.1.0
January 28, 2021

* Initial release of connected registry feature.
* Support for syncing single-level connected registry with the parent Azure Container Registry.

tags: `0.1.0`, `0.1.0-linux-amd64`, `0.1.0-linux-arm32v7`, `0.1.0-linux-arm64v8`
