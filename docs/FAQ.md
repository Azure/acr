# Azure Container Registry - Frequently Asked Questions

## Can I create Azure Container Registry using ARM Template?
Yes. Here is the template that you can use to create a registry - https://github.com/Azure/azure-cli/blob/master/src/command_modules/azure-cli-acr/azure/cli/command_modules/acr/template.json

## Is there security vulnerability scanning for images in ACR?

Yes. Please check the following links

Twistlock - https://www.twistlock.com/2016/11/07/twistlock-supports-azure-container-registry/

Aqua - http://blog.aquasec.com/image-vulnerability-scanning-in-azure-container-registry


## How to configure Kubernetes with Azure Container Registry?
http://kubernetes.io/docs/user-guide/images/#using-azure-container-registry-acr


## How to access Docker Registry HTTP API V2?
ACR supports Docker Registry HTTP API V2. The APIs can be accessed at
https://\<your registry login server\>/v2/

## Is Azure Premium Storage account supported?
Azure Premium Storage account is not supported.

## How to get login credentials for a container registry?

Please make sure admin is enabled.

Using `az cli`
```
az acr credential show -n myRegistry
```

Using `Azure Powershell`
```
Invoke-AzureRmResourceAction -Action listCredentials -ResourceType Microsoft.ContainerRegistry/registries -ResourceGroupName myResourceGroup -ResourceName myRegistry
```

## How to get login credentials in an ARM deployment template?

Please make sure admin is enabled.

```
{

"password": "[listCredentials(resourceId('Microsoft.ContainerRegistry/registries', 'myRegistry'), '2017-10-01').passwords[0].value]"
}
```

To get the second password

```
{
    "password": "[listCredentials(resourceId('Microsoft.ContainerRegistry/registries', 'myRegistry'), '2017-10-01').passwords[1].value]"
}
```

## How to update my registry to use the regenerated storage account access key?

Using `az cli` to update the storage account for your registry
```
az acr update -n myRegistry --storage-account-name myStorageAccount
```

Your can find `myStorageAccount` to your registry by the following command
```
az acr show -n myRegistry --query storageAccount
```

## How to delete all manifests that are not referenced by any tag in a repository?

If you are on bash
```
az acr repository show-manifests -n myRegistry --repository myRepository --query "[?tags==null].digest" -o tsv  | xargs -I% az acr repository delete -n myRegistry -t myRepository@%
```

For Powershell
```
az acr repository show-manifests -n myRegistry --repository myRepository --query "[?tags==null].digest" -o tsv | %{ az acr repository delete -n myRegistry -t myRepository@$_ }
```

Note: You can add `-y` in the delete command to skip confirmation

## How to log into my registry when running the CLI in a container?

You need to run the CLI container by mounting the Docker socket
```
docker run -it -v /var/run/docker.sock:/var/run/docker.sock azuresdk/azure-cli-python:dev
```

In the container, you can install `docker` by
```
apk update
apk add docker
```

Then you can log into your registry by
```
az acr login -n MyRegistry
```

## How to enable and get the debug logs of docker daemon?

* You need to start dockerd with debug option.

    First, create the docker daemon configuration file (`/etc/docker/daemon.json`) if it doesn't exist, and add the `debug` option:
    ```
    {
        "debug": true
    }
    ```
    Then, restart the daemon. For Ubuntu 14.04 user, you can do
    ```
    sudo service docker restart
    ```
    Details can be found [here](https://docs.docker.com/engine/admin/#enable-debugging).

* The logs may be generated at different locations, depending on your system. For example, for Ubuntu 14.04, it's `/var/log/upstart/docker.log`.
You can refer to [the link](https://docs.docker.com/engine/admin/#read-the-logs) for details:

* For Docker for Windows, the logs are generated under %LOCALAPPDATA%/docker/. However it may not contain all the debug information yet.
In order to access full daemon log, you may need some extra steps:
    ```
    docker run --privileged -it --rm -v /var/run/docker.sock:/var/run/docker.sock -v /usr/bin/docker:/usr/bin/docker alpine sh
    docker run --net=host --ipc=host --uts=host --pid=host -it --security-opt=seccomp=unconfined --privileged --rm -v /:/host alpine /bin/sh
    chroot /host
    ```

    Now you have access to all the files of the VM running dockerd. The log is at `/var/log/docker.log`.
    
## Does Azure Container Registry offer TLS v1.2 only configuration and how to enable TLS v1.2?

Yes. By using any latest docker client (version 18.03.0 and above). 
