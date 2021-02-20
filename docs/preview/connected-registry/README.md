# ACR connected registry (Private Preview) instructions

This article provides guidance for use of the connected registry feature of Azure Container Registry (ACR) during the limited preview. 

To request preview access, submit your contact details using this [form](https://forms.office.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR1OsLxas9SdIhfyFenqqkolUMkFKMTdDSU45SFQzU0o0WUNROVAySkRINy4u) and we will get in touch with you.

## Available Regions

During limited preview period, the connected registry functionality is available in dedicated stamps in the following Azure regions:

- Asia East
- EU West
- US East

To use the connected registry functionality, your ACR must be deployed in one of the above three regions and in a supported deployment stamp. To check the stamp where your ACR is deployed to, use the following command:

```azurecli
nslookup <your_registry_name>.azurecr.io
```

The stamp name is one of the aliases returned by the above command. Currently, connected registries are supported in the following stamps:

- East Asia: `ea-1.fe.azcr.io`
- EU West: `weu-3.fe.azcr.io`
- East US: `eus-2.fe.azcr.io`

> **IMPORTANT**
> If your ACR doesn't have the above alias respective to your region, the connected registry functionality will not be available. You can create an issue as described below, and we will migrate your registry to the correct stamp.

## Known Limitations

Here is a list of known limitations for the connected registry functionality in limited preview:

- Nested connected registry mode is still under development and requires additional testing. Currently nested registries are blocked and will be released in a few weeks.
- Number of tokens and scopemaps is limited to 20K for a single ACR. This indirectly limits the number of connected registries for an ACR registry because every connected registry needs a sync and client token.
- Number of repository permissions in a scope map is limited to 500.
- Number of clients for the connected registry is currently limited to 20.
- Image locking through repository/manifest/tag metadata is not currently supported for connected registries.
- Repository delete is not supported on the connected registry using registry mode.
- Audit logs for connected registries are currently not supported.
- Garbage collection of deleted artifacts on connected registries is currently not supported.
- Connected registry is coupled with home region data endpoint and its automatic migration for geo replications is not supported.
- Deletion of a connected registry needs manual removal of the containers on premises as well as removal of the respective scope map or tokens in the cloud.
- Connected registry sync limitations are as follows:
  - For continuous sync:
    - `minMessageTtl` is 1 day
    - `maxMessageTtl` is 90 days
  - For occasionally connected scenarios, where you want to specify sync window:
    - `minSyncWindow` is 1 hr
    - `maxSyncWindow` is 7 days

## Set Up and Configuration

In limited preview, the connected registry targets IoT scenarios. Below are links to the preliminary documentation you can use to set up and configure the connected registry with your IoT Edge infrastructure.

- [Overview of connected registry](./intro-connected-registry.md)
- [Understand access to a connected registry](./overview-connected-registry-access.md)
- [Using connected registry with Azure IoT Edge](./overview-connected-registry-and-iot-edge.md)
- [Quickstart: Create a connected registry using Azure Container Registry CLI commands](./quickstart-connected-registry-cli.md)
- [Quickstart: Deploy a connected registry to an IoT Edge device](./quickstart-deploy-connected-registry-iot-edge-cli.md)
- [Quickstart: Pull images from a connected registry](./quickstart-pull-images-from-connected-registry.md)

## Troubleshooting

We keep a list of troubleshooting steps for known issues. Those are available on the [Troubleshooting](./troubleshooting.md) page.

## Reporting Issues and Asking for Help

To report issues, [create a new bug](https://github.com/Azure/acr/issues/new?assignees=toddysm&labels=connected-registry,bug&template=bug_report.md&title=) in this repository.

If you need help with installation, set up, or use, you can [submit a help request](https://github.com/Azure/acr/issues/new?assignees=toddysm&labels=help%20wanted&template=bug_report.md&title=) in this repository.