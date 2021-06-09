---
title: Quickstart - Deploy a connected registry to an IoT Edge device
description: Use Azure Container Registry CLI commands and Azure portal to deploy a connected registry to an Azure IoT Edge device.
ms.topic: quickstart
ms.date: 12/04/2020
ms.author: memladen
author: toddysm
ms.custom:
---

# Quickstart: Deploy a connected registry to an IoT Edge device

In this quickstart, you use [Azure Container Registry][container-registry-intro] commands to deploy a connected registry to an Azure IoT Edge device. You can review the [ACR connected registry introduction](intro-connected-registry.md) for details about the connected registry feature of Azure Container Registry.

## Prerequisites

- Use [Azure Cloud Shell](https://docs.microsoft.com/en-us/azure/cloud-shell/quickstart) using the bash environment.
  
  [![https://docs.microsoft.com/en-us/azure/includes/media/cloud-shell-try-it/hdi-launch-cloud-shell.png](https://docs.microsoft.com/en-us/azure/includes/media/cloud-shell-try-it/hdi-launch-cloud-shell.png)](https://shell.azure.com/)
- If you prefer, [install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) the Azure CLI to run CLI reference commands.
  - If you're using a local install, sign in with Azure CLI by using the [az login](https://docs.microsoft.com/en-us/cli/azure/reference-index#az_login) command. To finish the authentication process, follow the steps displayed in your terminal. See [Sign in with Azure CLI](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) for additional sign-in options.
  - When you're prompted, install Azure CLI extensions on first use. For more information about extensions, see [Use extensions with Azure CLI](https://docs.microsoft.com/en-us/cli/azure/azure-cli-extensions-overview).
  - Run [az version](https://docs.microsoft.com/en-us/cli/azure/reference-index?#az_version) to find the version and dependent libraries that are installed. To upgrade to the latest version, run [az upgrade](https://docs.microsoft.com/en-us/cli/azure/reference-index?#az_upgrade).
- The Azure CLI commands in this article are formatted for the Bash shell. If you're using a different shell like PowerShell or Command Prompt, you may need to adjust line continuation characters or variable assignment lines accordingly. This article uses variables to minimize the amount of command editing required.

## Before you begin

This tutorial requires an Azure IoT Edge device to be set up upfront. You can use the [Deploy your first IoT Edge module to a virtual Linux device](https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/iot-edge/quickstart-linux.md) quickstart guide to learn how to deploy a virtual IoT Edge device. The connected registry is deployed as a module on the IoT Edge device. 

To install the latest 1.2 version of iotedge agent, login to the IoT device, open `/etc/iotedge/config.yaml`, search the section for `edgeAgent`, update the image version to 1.2.0 as the following.

```
agent:
  name: "edgeAgent"
  type: "docker"
  env: {}
  config:
    image: "mcr.microsoft.com/azureiotedge-agent:1.2"
    auth: {}
```

Save the config and restart the module using command `sudo systemctl restart iotedge`.

Also, make sure that you have created the connected registry resource in Azure as described in the [Create connected registry using the CLI][quickstart-connected-registry-cli] quickstart guide. Both, `registry` and `mirror` modes will work for this scenario.

## Import the connected registry image to your registry

To support nested IoT Edge scenarios, the container image for the connected registry runtime must be available in your private Azure Container Registry. Use the [az acr import][az-acr-import] command to import the connected registry image into your private registry.

```azurecli
az acr import \
  --name mycontainerregistry001 \
  --source mcr.microsoft.com/acr/connected-registry:0.2.0
```

To learn more about nested IoT Edge scenarios, please visit [Tutorial: Create a hierarchy of IoT Edge devices](https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/iot-edge/tutorial-nested-iot-edge.md).

## Import the IotEdge and API Proxy images into your registry

Import the images azureiotedge-api-proxy, azureiotedge-agent, azureiotedge-hub from mcr into your private registry using the same command as above. For security purpose, for top level device, you can pull those images from MCR but it is recommended to import those images into your private registry so your deployment can pull the images from your own repo.

## Create a client token for access to the cloud registry

The IoT Edge runtime will need to authenticate with the cloud registry to pull the images and deploy it. First, use the following command to create a scope map for the iotedge, api proxy and connected registry image repository:

```azurecli
az acr scope-map create \
  --description "Connected registry repo pull scope map." \
  --name conected-registry-pull \
  --registry mycontainerregistry001 \
  --repository "acr/connected-registry" "azureiotedge-api-proxy" "azureiotedge-agent" "azureiotedge-hub" content/read
```

Next, use the following command to create a client token for the IoT Edge device and associate it to the scope map:

```azurecli
az acr token create \
  --name crimagepulltoken \
  --registry mycontainerregistry001 \
  --scope-map conected-registry-pull
```

This command will print a JSON that will include credential information similar to the following:

```json
  ...
  "credentials": {
    "activeDirectoryObject": null,
    "certificates": [],
    "passwords": [
      {
        "creationTime": "2020-12-10T00:06:15.356846+00:00",
        "expiry": null,
        "name": "password1",
        "value": "$$$0meCoMPL3xP4$$W0rd001!@#$$"
      },
      {
        "creationTime": "2020-12-10T00:06:15.356846+00:00",
        "expiry": null,
        "name": "password2",
        "value": "#$an0TH3rCoMPL3xP4ssW0rd002!#$"
      }
    ],
    "username": "crimagepulltoken"
  }
  ...
```

You will need the `username` and one of the `passwords` values for the IoT Edge manifest below.

  > [!IMPORTANT]
  > Make sure that you save the generated passwords. Those are one-time passwords and cannot be retrieved. You can generate new passwords using the [az acr token credential generate][az-acr-token-credential-generate] command.

More details about tokens and scope maps are available in [Create a token with repository-scoped permissions](container-registry-repository-scoped-permissions.md).

## Retrieve connected registry configuration information

Before deploying the connected registry to the IoT Edge device, you will need to retrieve the configuration from the connected registry resource in Azure. Use the [az acr connected-registry install][az-acr-connected-registry-install] command to retrieve the configuration.

```azurecli
az acr connected-registry install renew-credentials \
  --registry mycontainerregistry001 \
  --name myconnectedregistry \
```

This will return the connection string for the connected registry including the newly generated passwords.

```json
{
  "ACR_REGISTRY_CONNECTION_STRING": "ConnectedRegistryName=myconnectedregistry;SyncTokenName=myconnectedregistry-sync-token;SyncTokenPassword=s0meCoMPL3xP4$$W0rd001!@#;ParentGatewayEndpoint=mycontainerregistry001.westus2.data.azurecr.io;ParentEndpointProtocol=https",
  "ACR_REGISTRY_LOGIN_SERVER": "<Optional: connected registry login server. More info at https://aka.ms/acr/connected-registry>"
}
```

The JSON above lists the environment variables that need to be passed to the connected registry container at run time. The following environment variables are optional:

- `ACR_REGISTRY_CERTIFICATE_VOLUME` - this is required if your connected registry will be accessible via HTTPS. The volume should point to the location where the HTTPS certificates are stored. If not set, the default location is `/var/acr/certs`.
- `ACR_REGISTRY_DATA_VOLUME` - this can optionally be used to overwrite the default location `/var/acr/data` where the images will be stored by the connected registry. This location must match the volume bind for the container.

You will need the information for the IoT Edge manifest below.

  > [!IMPORTANT]
  > Make sure that you save the generated passwords. Those are one-time passwords and cannot be retrieved. If you issue the command again, new passwords will be generated. You can generate new passwords using the [az acr token credential generate][az-acr-token-credential-generate] command.

## Configure a deployment manifest for IoT Edge

A deployment manifest is a JSON document that describes which modules to deploy to the IoT Edge device. For more information about how deployment manifests work and how to create them, see [Understand how IoT Edge modules can be used, configured, and reused](https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/iot-edge/module-composition.md).

To deploy the connected registry and api proxy module using the Azure CLI, save the following deployment manifest locally as a `.json` file. 

```json
{
    "modulesContent": {
        "$edgeAgent": {
            "properties.desired": {
                "modules": {
                    "connected-registry": {
                        "settings": {
                            "image": "mycontainerregistry001.azurecr.io/acr/connected-registry:0.2.0",
                            "createOptions": "{\"HostConfig\":{\"Binds\":[\"/home/azureuser/connected-registry:/var/acr/data\"],\"PortBindings\":{\"8080/tcp\":[{\"HostPort\":\"8080\"}]}}}"
                        },
                        "type": "docker",
                        "env": {
                            "ACR_REGISTRY_CONNECTION_STRING": {
                                "value": "ConnectedRegistryName=myconnectedregistry;SyncTokenName=myconnectedregistry-sync-token;SyncTokenPassword=s0meCoMPL3xP4$$W0rd001!@#;ParentGatewayEndpoint=mycontainerregistry001.westus2.data.azurecr.io;ParentEndpointProtocol=https"
                            }
                        },
                        "status": "running",
                        "restartPolicy": "always",
                        "version": "1.0"
                    },
                    "IoTEdgeAPIProxy": {
                        "settings": {
                            "image": "mycontainerregistry001.azurecr.io/azureiotedge-api-proxy:1.0",
                            "createOptions": "{\"HostConfig\":{\"PortBindings\":{\"8000/tcp\":[{\"HostPort\":\"8000\"}]}}}"
                        },
                        "type": "docker",
                        "env": {
                            "NGINX_DEFAULT_PORT": {
                                "value": "8000"
                            },
                            "CONNECTED_ACR_ROUTE_ADDRESS": {
                                "value": "connected-registry:8080"
                            },
                            "BLOB_UPLOAD_ROUTE_ADDRESS": {
                                "value": "AzureBlobStorageonIoTEdge:11002"
                            }
                        },
                        "status": "running",
                        "restartPolicy": "always",
                        "version": "1.0"
                    }
                },
                "runtime": {
                    "settings": {
                        "minDockerVersion": "v1.25",
                        "registryCredentials": {
                            "tsmregistry": {
                                "address": "mycontainerregistry001.azurecr.io",
                                "password": "$$$0meCoMPL3xP4$$W0rd001!@#$$",
                                "username": "crimagepulltoken"
                            }
                        }
                    },
                    "type": "docker"
                },
                "schemaVersion": "1.1",
                "systemModules": {
                    "edgeAgent": {
                        "settings": {
                            "image": "mycontainerregistry001.azurecr.io/azureiotedge-agent:1.2",
                            "createOptions": ""
                        },
                        "type": "docker",
                        "env": {
                            "SendRuntimeQualityTelemetry": {
                                "value": "false"
                            }
                        }
                    },
                    "edgeHub": {
                        "settings": {
                            "image": "mycontainerregistry001.azurecr.io/azureiotedge-hub:1.2",
                            "createOptions": "{\"HostConfig\":{\"PortBindings\":{\"443/tcp\":[{\"HostPort\":\"443\"}],\"5671/tcp\":[{\"HostPort\":\"5671\"}],\"8883/tcp\":[{\"HostPort\":\"8883\"}]}}}"
                        },
                        "type": "docker",
                        "status": "running",
                        "restartPolicy": "always"
                    }
                }
            }
        },
        "$edgeHub": {
            "properties.desired": {
                "routes": {
                    "route": "FROM /messages/* INTO $upstream"
                },
                "schemaVersion": "1.1",
                "storeAndForwardConfiguration": {
                    "timeToLiveSecs": 7200
                }
            }
        }
    }
}
```

  > [!IMPORTANT]
  > If the connected registry listens on a port different from 80 and 443, the `ACR_REGISTRY_LOGIN_SERVER` value must include the port, eg. `192.168.0.100:8080`.

Use the information from the previous sections to update the relevant JSON values.

You will use the file path in the next section when you run the command to apply the configuration to your device.

## Deploy the connected registry and api proxy modules on IoT Edge

Use the following command to deploy the connected registry and api proxy modules on the IoT Edge device:

```azurecli
az iot edge set-modules \
  --device-id [device id] \
  --hub-name [hub name] \
  --content [file path]
```

For more details you can refer to the [Deploy Azure IoT Edge modules with Azure CLI](https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/iot-edge/how-to-deploy-modules-cli.md) article.

To check the status of the connected registry, use the following CLI command:

```azurecli
az acr connected-registry show \
  --registry mycontainerregistry001 \
  --name myconnectedregistry \
  --output table
```

You may need to a wait few minutes until the deployment of the connected registry and api proxy complete.

Make sure you open the the ports `8000`, `5671`, `8883`. The api proxy will listen on port 8000 configued as `NGINX_DEFAULT_PORT`.

You can find more information about API Proxy in the [https://github.com/Azure/iotedge/tree/master/edge-modules/api-proxy-module]

## Next steps

In this quickstart, you learned how to deploy a connected registry to an IoT Edge device. Continue to the next guide to learn how to pull images from the newly deployed connected registry.

> [Quickstart: Pull images from a connected registry][quickstart-pull-images-from-connected-registry]

> [Quickstart: Deploy connected registry on nested IoT Edge device][quickstart-connected-registry-nested]

<!-- LINKS - internal -->
[az-acr-connected-registry-install]: https://docs.microsoft.com/cli/azure/acr/connected-registry/install?view=azure-cli-latest
[az-acr-import]: https://docs.microsoft.com/cli/azure/acr?view=azure-cli-latest#az_acr_import
[az-acr-token-credential-generate]: https://docs.microsoft.com/cli/azure/acr/token/credential?view=azure-cli-latest#az_acr_token_credential_generate
[container-registry-intro]: container-registry-intro.md
[quickstart-pull-images-from-connected-registry]: quickstart-pull-images-from-connected-registry.md
[quickstart-connected-registry-cli]: quickstart-connected-registry-cli.md
[quickstart-connected-registry-nested]: quickstart-deploy-connected-registry-nested-iot-edge-cli.md
