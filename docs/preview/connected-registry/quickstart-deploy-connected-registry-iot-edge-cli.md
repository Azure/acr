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

## Create a client token for access to the cloud registry

The IoT Edge runtime will need to authenticate with the cloud registry to pull the connected registry image and deploy it. First, use the following command to create a scope map for the connected registry image repository:

```azurecli
az acr scope-map create \
  --description "Connected registry repo pull scope map." \
  --name conected-registry-pull \
  --registry mycontainerregistry001 \
  --repository "acr/connected-registry" content/read
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

To deploy the connected registry module using the Azure CLI, save the following deployment manifest locally as a `.json` file. 

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
                            "image": "mcr.microsoft.com/azureiotedge-agent:1.2",
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
                            "image": "mcr.microsoft.com/azureiotedge-hub:1.2",
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

## Deploy the connected registry module on IoT Edge

Use the following command to deploy the connected registry module on the IoT Edge device:

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

You may need to a wait few minutes until the deployment of the connected registry completes.

## Deploy the api proxy module on IoT Edge

Add api proxy module from Azure Marketplace `IoT Edge API Proxy` as described in the [https://docs.microsoft.com/en-us/azure/iot-edge/how-to-configure-api-proxy-module?view=iotedge-2020-11]

Remove the existing env DOCKER_REQUEST_ROUTE_ADDRESS.

Add the following two environment variables:

```
"CONNECTED_ACR_ROUTE_ADDRESS": {
      "value": "connected-registry:8080"
},
"NGINX_CONFIG_ENV_VAR_LIST": {
        "value": "NGINX_DEFAULT_PORT,BLOB_UPLOAD_ROUTE_ADDRESS,CONNECTED_ACR_ROUTE_ADDRESS,IOTEDGE_PARENTHOSTNAME"
}
```

Update the proxy config for the connected registry following the steps:
- Click into the api proxy module from the portal.
- Click `Module Identity Twin`
- Add `proxy_config` in the desired propeties as the following.

```
"desired": {
            "proxy_config": "ZXZlbnRzIHsgfQoKCmh0dHAgewogICAgcHJveHlfYnVmZmVycyAzMiAxNjBrOwogICAgcHJveHlfYnVmZmVyX3NpemUgMTYwazsKICAgIHByb3h5X3JlYWRfdGltZW91dCAzNjAwOwogICAgZXJyb3JfbG9nIC9kZXYvc3Rkb3V0IGluZm87CiAgICBhY2Nlc3NfbG9nIC9kZXYvc3Rkb3V0OwoKICAgIHNlcnZlciB7CiAgICAgICAgbGlzdGVuICR7TkdJTlhfREVGQVVMVF9QT1JUfSBzc2wgZGVmYXVsdF9zZXJ2ZXI7CgogICAgICAgIGNodW5rZWRfdHJhbnNmZXJfZW5jb2Rpbmcgb247CgogICAgICAgIHNzbF9jZXJ0aWZpY2F0ZSAgICAgICAgc2VydmVyLmNydDsKICAgICAgICBzc2xfY2VydGlmaWNhdGVfa2V5ICAgIHByaXZhdGVfa2V5X3NlcnZlci5wZW07CiAgICAgICAgc3NsX2NsaWVudF9jZXJ0aWZpY2F0ZSB0cnVzdGVkQ0EuY3J0OwogICAgICAgICNzc2xfdmVyaWZ5X2RlcHRoIDc7CiAgICAgICAgc3NsX3ZlcmlmeV9jbGllbnQgb3B0aW9uYWxfbm9fY2E7CgogICAgICAgICNpZl90YWcgJHtCTE9CX1VQTE9BRF9ST1VURV9BRERSRVNTfQogICAgICAgIGlmICgkaHR0cF94X21zX3ZlcnNpb24pCiAgICAgICAgewogICAgICAgICAgICByZXdyaXRlIF4oLiopJCAvc3RvcmFnZSQxIGxhc3Q7CiAgICAgICAgfQogICAgICAgICNlbmRpZl90YWcgJHtCTE9CX1VQTE9BRF9ST1VURV9BRERSRVNTfQogICAgICAgICNpZl90YWcgISR7QkxPQl9VUExPQURfUk9VVEVfQUREUkVTU30KICAgICAgICBpZiAoJGh0dHBfeF9tc192ZXJzaW9uKQogICAgICAgIHsKICAgICAgICAgICAgcmV3cml0ZSBeKC4qKSQgL3BhcmVudCQxIGxhc3Q7CiAgICAgICAgfQogICAgICAgICNlbmRpZl90YWcgJHtCTE9CX1VQTE9BRF9ST1VURV9BRERSRVNTfQoKICAgICAgICAjaWZfdGFnICR7QkxPQl9VUExPQURfUk9VVEVfQUREUkVTU30KICAgICAgICBsb2NhdGlvbiB+Xi9zdG9yYWdlLyguKil7CiAgICAgICAgICAgIHJlc29sdmVyIDEyNy4wLjAuMTE7CiAgICAgICAgICAgIHByb3h5X2h0dHBfdmVyc2lvbiAxLjE7CiAgICAgICAgICAgIHByb3h5X3Bhc3MgICAgICAgICAgaHR0cDovLyR7QkxPQl9VUExPQURfUk9VVEVfQUREUkVTU30vJDEkaXNfYXJncyRhcmdzOwogICAgICAgIH0KICAgICAgICAjZW5kaWZfdGFnICR7QkxPQl9VUExPQURfUk9VVEVfQUREUkVTU30KCiAgICAgICAgI2lmX3RhZyAke0NPTk5FQ1RFRF9BQ1JfUk9VVEVfQUREUkVTU30KICAgICAgICBsb2NhdGlvbiAvdjIgewogICAgICAgICAgICBjbGllbnRfbWF4X2JvZHlfc2l6ZSAxMDAwRzsKICAgICAgICAgICAgcmVzb2x2ZXIgMTI3LjAuMC4xMTsKICAgICAgICAgICAgcHJveHlfaHR0cF92ZXJzaW9uIDEuMTsKICAgICAgICAgICAgcHJveHlfcGFzcyAgICAgICAgIGh0dHA6Ly8ke0NPTk5FQ1RFRF9BQ1JfUk9VVEVfQUREUkVTU307CiAgICAgICAgICAgIHByb3h5X3NldF9oZWFkZXIgICBYLUZvcndhcmRlZC1Ib3N0ICRodHRwX2hvc3Q7CiAgICAgICAgICAgIHByb3h5X3NldF9oZWFkZXIgICBYLUZvcndhcmRlZC1Qcm90byAkc2NoZW1lOwogICAgICAgIH0KCiAgICAgICAgbG9jYXRpb24gL2FjciB7CiAgICAgICAgICAgIGNsaWVudF9tYXhfYm9keV9zaXplIDEwTTsKICAgICAgICAgICAgcmVzb2x2ZXIgMTI3LjAuMC4xMTsKICAgICAgICAgICAgcHJveHlfaHR0cF92ZXJzaW9uIDEuMTsKICAgICAgICAgICAgcHJveHlfcGFzcyAgICAgICAgIGh0dHA6Ly8ke0NPTk5FQ1RFRF9BQ1JfUk9VVEVfQUREUkVTU307CiAgICAgICAgICAgIHByb3h5X3NldF9oZWFkZXIgICBYLUZvcndhcmRlZC1Ib3N0ICRodHRwX2hvc3Q7CiAgICAgICAgICAgIHByb3h5X3NldF9oZWFkZXIgICBYLUZvcndhcmRlZC1Qcm90byAkc2NoZW1lOwogICAgICAgIH0KICAgICAgICAjZW5kaWZfdGFnICR7Q09OTkVDVEVEX0FDUl9ST1VURV9BRERSRVNTfQoKICAgICAgICAjaWZfdGFnICR7SU9URURHRV9QQVJFTlRIT1NUTkFNRX0KICAgICAgICBsb2NhdGlvbiB+Xi9wYXJlbnQvKC4qKSB7CiAgICAgICAgICAgIHByb3h5X2h0dHBfdmVyc2lvbiAxLjE7CiAgICAgICAgICAgIHJlc29sdmVyIDEyNy4wLjAuMTE7CiAgICAgICAgICAgICNwcm94eV9zc2xfY2VydGlmaWNhdGUgICAgIGlkZW50aXR5LmNydDsKICAgICAgICAgICAgI3Byb3h5X3NzbF9jZXJ0aWZpY2F0ZV9rZXkgcHJpdmF0ZV9rZXlfaWRlbnRpdHkucGVtOwogICAgICAgICAgICBwcm94eV9zc2xfdHJ1c3RlZF9jZXJ0aWZpY2F0ZSB0cnVzdGVkQ0EuY3J0OwogICAgICAgICAgICBwcm94eV9zc2xfdmVyaWZ5X2RlcHRoIDc7CiAgICAgICAgICAgIHByb3h5X3NzbF92ZXJpZnkgICAgICAgb247CiAgICAgICAgICAgIHByb3h5X3Bhc3MgICAgICAgICAgaHR0cHM6Ly8ke0lPVEVER0VfUEFSRU5USE9TVE5BTUV9OiR7TkdJTlhfREVGQVVMVF9QT1JUfS8kMSRpc19hcmdzJGFyZ3M7CiAgICAgICAgfQogICAgICAgICNlbmRpZl90YWcgJHtJT1RFREdFX1BBUkVOVEhPU1ROQU1FfQoKICAgICAgICBsb2NhdGlvbiB+Xi9kZXZpY2VzfHR3aW5zLyB7CiAgICAgICAgICAgIHByb3h5X2h0dHBfdmVyc2lvbiAgMS4xOwogICAgICAgICAgICBwcm94eV9zc2xfdmVyaWZ5ICAgIG9mZjsKICAgICAgICAgICAgcHJveHlfc2V0X2hlYWRlciAgICB4LW1zLWVkZ2UtY2xpZW50Y2VydCAgICAkc3NsX2NsaWVudF9lc2NhcGVkX2NlcnQ7CiAgICAgICAgICAgIHByb3h5X3Bhc3MgICAgICAgICAgaHR0cHM6Ly9lZGdlSHViOwogICAgICAgIH0KICAgIH0KfQ==",

            "$metadata": {...}
```
The value of the proxy_config is the base64 encoded string of the following nginx config.

``` nginx
events { }


http {
    proxy_buffers 32 160k;
    proxy_buffer_size 160k;
    proxy_read_timeout 3600;
    error_log /dev/stdout info;
    access_log /dev/stdout;

    server {
        listen ${NGINX_DEFAULT_PORT} ssl default_server;

        chunked_transfer_encoding on;

        ssl_certificate        server.crt;
        ssl_certificate_key    private_key_server.pem;
        ssl_client_certificate trustedCA.crt;
        #ssl_verify_depth 7;
        ssl_verify_client optional_no_ca;

        #if_tag ${BLOB_UPLOAD_ROUTE_ADDRESS}
        if ($http_x_ms_version)
        {
            rewrite ^(.*)$ /storage$1 last;
        }
        #endif_tag ${BLOB_UPLOAD_ROUTE_ADDRESS}
        #if_tag !${BLOB_UPLOAD_ROUTE_ADDRESS}
        if ($http_x_ms_version)
        {
            rewrite ^(.*)$ /parent$1 last;
        }
        #endif_tag ${BLOB_UPLOAD_ROUTE_ADDRESS}

        #if_tag ${BLOB_UPLOAD_ROUTE_ADDRESS}
        location ~^/storage/(.*){
            resolver 127.0.0.11;
            proxy_http_version 1.1;
            proxy_pass          http://${BLOB_UPLOAD_ROUTE_ADDRESS}/$1$is_args$args;
        }
        #endif_tag ${BLOB_UPLOAD_ROUTE_ADDRESS}

        #if_tag ${CONNECTED_ACR_ROUTE_ADDRESS}
        location /v2 {
            client_max_body_size 1000G;
            resolver 127.0.0.11;
            proxy_http_version 1.1;
            proxy_pass         http://${CONNECTED_ACR_ROUTE_ADDRESS};
            proxy_set_header   X-Forwarded-Host $http_host;
            proxy_set_header   X-Forwarded-Proto $scheme;
        }

        location /acr {
            client_max_body_size 10M;
            resolver 127.0.0.11;
            proxy_http_version 1.1;
            proxy_pass         http://${CONNECTED_ACR_ROUTE_ADDRESS};
            proxy_set_header   X-Forwarded-Host $http_host;
            proxy_set_header   X-Forwarded-Proto $scheme;
        }
        #endif_tag ${CONNECTED_ACR_ROUTE_ADDRESS}

        #if_tag ${IOTEDGE_PARENTHOSTNAME}
        location ~^/parent/(.*) {
            proxy_http_version 1.1;
            resolver 127.0.0.11;
            #proxy_ssl_certificate     identity.crt;
            #proxy_ssl_certificate_key private_key_identity.pem;
            proxy_ssl_trusted_certificate trustedCA.crt;
            proxy_ssl_verify_depth 7;
            proxy_ssl_verify       on;
            proxy_pass          https://${IOTEDGE_PARENTHOSTNAME}:${NGINX_DEFAULT_PORT}/$1$is_args$args;
        }
        #endif_tag ${IOTEDGE_PARENTHOSTNAME}

        location ~^/devices|twins/ {
            proxy_http_version  1.1;
            proxy_ssl_verify    off;
            proxy_set_header    x-ms-edge-clientcert    $ssl_client_escaped_cert;
            proxy_pass          https://edgeHub;
        }
    }
}
```

- Click 'Save'

Make sure you open the the ports `8000`, `5671`, `8883`.

The api proxy will now listen on port 8000 configued as `NGINX_DEFAULT_PORT`.

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
