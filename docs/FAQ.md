# Azure Container Registry - Frequently Asked Questions

<!-- TOC depthFrom:2 orderedList:false -->

- [Resource Management](#resource-management)
    - [Can I create Azure Container Registry using ARM Template?](#can-i-create-azure-container-registry-using-arm-template)
    - [Is there security vulnerability scanning for images in ACR?](#is-there-security-vulnerability-scanning-for-images-in-acr)
    - [How to configure Kubernetes with Azure Container Registry?](#how-to-configure-kubernetes-with-azure-container-registry)
    - [Is Azure Premium Storage account supported?](#is-azure-premium-storage-account-supported)
    - [How to get login credentials for a container registry?](#how-to-get-login-credentials-for-a-container-registry)
    - [How to get login credentials in an ARM deployment template?](#how-to-get-login-credentials-in-an-arm-deployment-template)
    - [How to update my registry to use the regenerated storage account access key?](#how-to-update-my-registry-to-use-the-regenerated-storage-account-access-key)
    - [Delete of replication fails with Forbidden status , although the replication gets deleted using CLI or Remove-AzureRmContainerRegistryReplication.](#delete-of-replication-fails-with-forbidden-status--although-the-replication-gets-deleted-using-cli-or-remove-azurermcontainerregistryreplication)
- [Registry Operations](#registry-operations)
    - [How to access Docker Registry HTTP API V2?](#how-to-access-docker-registry-http-api-v2)
    - [How to delete all manifests that are not referenced by any tag in a repository?](#how-to-delete-all-manifests-that-are-not-referenced-by-any-tag-in-a-repository)
    - [Why does the registry usage quota does not reduce after deleting images?](#why-does-the-registry-usage-quota-does-not-reduce-after-deleting-images)
    - [How do I validate storage quota changes?](#how-do-i-validate-storage-quota-changes)
    - [How to log into my registry when running the CLI in a container?](#how-to-log-into-my-registry-when-running-the-cli-in-a-container)
    - [Does Azure Container Registry offer TLS v1.2 only configuration and how to enable TLS v1.2?](#does-azure-container-registry-offer-tls-v12-only-configuration-and-how-to-enable-tls-v12)
    - [Does Azure Container Registry support Content Trust?](#does-azure-container-registry-support-content-trust)
        - [Where is the file for the thumbprint located ?](#where-is-the-file-for-the-thumbprint-located-)
    - [How to grant access to pull or push images without the permission to manage the registry resource](#how-to-grant-access-to-pull-or-push-images-without-the-permission-to-manage-the-registry-resource)
    - [How to enable automatic image quarantine for a registry](#how-to-enable-automatic-image-quarantine-for-a-registry)
- [Diagnostics](#diagnostics)
    - [docker pull fails with error: net/http: request canceled while waiting for connection (Client.Timeout exceeded while awaiting headers)](#docker-pull-fails-with-error-nethttp-request-canceled-while-waiting-for-connection-clienttimeout-exceeded-while-awaiting-headers)
    - [docker push succeeds but docker pull fails with error: unauthorized: authentication required](#docker-push-succeeds-but-docker-pull-fails-with-error-unauthorized-authentication-required)
    - [How to enable and get the debug logs of docker daemon?](#how-to-enable-and-get-the-debug-logs-of-docker-daemon)	
    - [New user permissions may not be effective immediately after updating](#new-user-permissions-may-not-be-effective-immediately-after-updating)
- [Tasks](#tasks)
    - [How to batch cancel runs?](#how-to-batch-cancel-runs)
    - [How to include .git folder in az acr build command?](#how-to-include-git-folder-in-az-acr-build-command)

<!-- /TOC -->

## Resource Management

### Can I create Azure Container Registry using ARM Template?
Yes. Here is the template that you can use to create a registry - https://github.com/Azure/azure-cli/blob/master/src/command_modules/azure-cli-acr/azure/cli/command_modules/acr/template.json

### Is there security vulnerability scanning for images in ACR?

Yes. Please check the following links

Twistlock - https://www.twistlock.com/2016/11/07/twistlock-supports-azure-container-registry/

Aqua - http://blog.aquasec.com/image-vulnerability-scanning-in-azure-container-registry


### How to configure Kubernetes with Azure Container Registry?
http://kubernetes.io/docs/user-guide/images/#using-azure-container-registry-acr


### Is Azure Premium Storage account supported?
Azure Premium Storage account is not supported.

### How to get login credentials for a container registry?

Please make sure admin is enabled.

Using `az cli`
```
az acr credential show -n myRegistry
```

Using `Azure Powershell`
```
Invoke-AzureRmResourceAction -Action listCredentials -ResourceType Microsoft.ContainerRegistry/registries -ResourceGroupName myResourceGroup -ResourceName myRegistry
```

### How to get login credentials in an ARM deployment template?

Please make sure admin is enabled.

```json
{
    "password": "[listCredentials(resourceId('Microsoft.ContainerRegistry/registries', 'myRegistry'), '2017-10-01').passwords[0].value]"
}
```

To get the second password

```json
{
    "password": "[listCredentials(resourceId('Microsoft.ContainerRegistry/registries', 'myRegistry'), '2017-10-01').passwords[1].value]"
}
```

### How to update my registry to use the regenerated storage account access key?

Using `az cli` to update the storage account for your registry

```bash
az acr update -n myRegistry --storage-account-name myStorageAccount
```

Your can find `myStorageAccount` to your registry by the following command

```bash
az acr show -n myRegistry --query storageAccount
```

### Delete of replication fails with Forbidden status , although the replication gets deleted using CLI or Remove-AzureRmContainerRegistryReplication.

The error is usually seen when the user has permissions on a Registry but doesn't have reader level permission on the subscription. To resolve this issue

Assign the user the reader permission on the subscription. 

```bash  
az role assignment create --role "Reader" --assignee user@contoso.com --scope /subscriptions/<subscription_id> 
```

## Registry Operations

### How to access Docker Registry HTTP API V2?

ACR supports Docker Registry HTTP API V2. The APIs can be accessed at
https://\<your registry login server\>/v2/

### How to delete all manifests that are not referenced by any tag in a repository?

If you are on bash

```bash
az acr repository show-manifests -n myRegistry --repository myRepository --query "[?tags[0]==null].digest" -o tsv  | xargs -I% az acr repository delete -n myRegistry -t myRepository@%
```

For Powershell

```powershell
az acr repository show-manifests -n myRegistry --repository myRepository --query "[?tags[0]==null].digest" -o tsv | %{ az acr repository delete -n myRegistry -t myRepository@$_ }
```

Note: You can add `-y` in the delete command to skip confirmation

### Why does the registry usage quota does not reduce after deleting images?

This can happen if the underlying layers are still being referenced by other container images. If you delete an image with no references, the registry usage will be updated in a few minutes.

### How do I validate storage quota changes?

Create an image with a 1GB layer using the following docker file. This ensures that the image has has a layer that is not shared by any other image in the registry.

```dockerfile
FROM alpine
RUN dd if=/dev/urandom of=1GB.bin  bs=32M  count=32
RUN ls -lh 1GB.bin
```

Build and push the image to your registry using the docker CLI.

```bash
docker build -t myregistry.azurecr.io/1gb:latest .
docker push myregistry.azurecr.io/1gb:latest
```

You should be able to see that the storage used, has increased in the portal or you can query usage using the CLI.

```bash
az acr show-usage -n myregistry
```

Delete the image using the Azure CLI or portal and check the updated usage in a few minutes.

```bash
az acr repository delete -n myregistry --image 1gb
```

### How to log into my registry when running the CLI in a container?

You need to run the CLI container by mounting the Docker socket
```
docker run -it -v /var/run/docker.sock:/var/run/docker.sock azuresdk/azure-cli-python:dev
```

In the container, you can install `docker` by
```
apk --update add docker
```

Then you can log into your registry by
```
az acr login -n MyRegistry
```


### Does Azure Container Registry offer TLS v1.2 only configuration and how to enable TLS v1.2?

Yes. By using any latest docker client (version 18.03.0 and above). 

### Does Azure Container Registry support Content Trust?

Yes, you can use trusted images in Azure Container Registry as the [Docker Notary](https://docs.docker.com/notary/getting_started/) has been integrited into ACR and can be enabled.

* To push trusted images, you need to grant yourself or related service principles the `AcrImageSigner` role scoped to your registry, in addition to the `Contributor` (or `Owner`) role for signing. Role assignment can be done by the following methods.
    * Azure Portal: Your registry -> Access Control (IAM) -> Add (Select `AcrImageSigner` for the Role).
    * Azure CLI: Find the resource id `id` of the registry by running
        ```
        az acr show -n myRegistry
        ```
        Then you can assign the `AcrImageSigner` role to a user
        ```
        az role assignment create --scope resource_id --role AcrImageSigner --assignee user@example.com
        ```
        or a service principle identified by its application ID
        ```
        az role assignment create --scope resource_id --role AcrImageSigner --assignee 00000000-0000-0000-0000-000000000000
        ```
* To pull trusted images, a `Reader` role is enough for normal users. No additional roles like an `AcrImageSigner` role are required.

After any role changes, please run `az acr login` to refresh the local identity token so that the new roles can take effect.

You can use Docker Client and Notary Client to interact trusted images with ACR.
Detailed documentation can be found at [Content trust in Docker](https://docs.docker.com/engine/security/trust/content_trust/).

####  Where is the file for the thumbprint located ?

Under `~/.docker/trust/tuf/myregistry.azurecr.io/myrepository/metadata`,

* Public keys / certificates of all roles (except delegation roles) are stored in the `root.json`.
* Public keys / certificates of the delegation role are stored in the json file of its parent role (for example `targets.json` for the `targets/releases` role).

It is suggested to verify those public keys / certificates after the overall TUF verification done by the Docker / Notary client.

### How to grant access to pull or push images without the permission to manage the registry resource

ACR supports [custom roles](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-roles) that provide different levels of permissions. Specifically, `AcrPull` and `AcrPush` roles allow users to pull and/or push images without the permission to manage the registry resource in Azure.

* Azure Portal: Your registry -> Access Control (IAM) -> Add (Select `AcrPull` or `AcrPush` for the Role).
* Azure CLI: Find the resource id `id` of the registry by running
    ```
    az acr show -n myRegistry
    ```
    Then you can assign the `AcrPull` or `AcrPush` role to a user (the following example uses `AcrPull`)
    ```
    az role assignment create --scope resource_id --role AcrPull --assignee user@example.com
    ```
    or a service principle identified by its application ID
    ```
    az role assignment create --scope resource_id --role AcrPull --assignee 00000000-0000-0000-0000-000000000000
    ```

The assignee is then able to login and access images in the registry.

* To login to a registry:
    ```
    az acr login -n myRegistry
    ```
* To list repositories:
    ```
    az acr repository list -n myRegistry
    ```
* To pull an image:
    ```
    docker pull myregistry.azurecr.io/hello-world
    ```

Note that with the use of only `AcrPull` or `AcrPush` roles, the assignee doesn't have the permission to manage the registry resource in Azure. For example, `az acr list` or `az acr show -n myRegistry` won't show the registry.

### How to enable automatic image quarantine for a registry

Image quarantine is currently a preview feature of ACR. You can enable the Quarantine mode of a registry so that only those images which have successfully passed security scan can be visible to normal users. You can find more details [here](https://github.com/Azure/acr/tree/master/docs/preview/quarantine)


## Diagnostics

### docker pull fails with error: net/http: request canceled while waiting for connection (Client.Timeout exceeded while awaiting headers)

 - If this error is a transient issue, then retry will succeed. 
 - If it is failing continuously then there could be a problem with the docker daemon, which can be mitigated by restarting the docker daemon. We have seen such issues before and restarting daemon generally works.
 - If you continue to see this issue after restarting docker daemon, then the problem could be some network connectivity issues with the machine. To check if general network on the machine is healthy, try pinging www.bing.com and see if it works.
 - You should always have a retry mechanism on all docker client operations.

### docker push succeeds but docker pull fails with error: unauthorized: authentication required

This error usually happens with the Red Hat version of docker daemon where `--signature-verification` is enabled by default. You can check the docker daemon options for Red Hat Enterprise Linux (RHEL) or Fedora by running
```
grep OPTIONS /etc/sysconfig/docker
```

For instance, **Fedora 28 Server** has the docker daemon options
```
OPTIONS='--selinux-enabled --log-driver=journald --live-restore'
```

With `--signature-verification=false` missing, you will experience docker pull failures like
```
Trying to pull repository myregistry.azurecr.io/myimage ...
unauthorized: authentication required
```

To resolve the error,
1. Add the option `--signature-verification=false` to the docker daemon configuration file `/etc/sysconfig/docker`. For example,
   ```
   OPTIONS='--selinux-enabled --log-driver=journald --live-restore --signature-verification=false'
   ```
2. Restart docker daemon service by running
   ```
   sudo systemctl restart docker.service
   ```

Details of `--signature-verification` can be found by running `man dockerd`.

### How to enable and get the debug logs of docker daemon?	


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
    docker run --privileged -it --rm -v /var/run/docker.sock:/var/run/docker.sock -v /usr/local/bin/docker:/usr/local/bin/docker alpine sh
    docker run --net=host --ipc=host --uts=host --pid=host -it --security-opt=seccomp=unconfined --privileged --rm -v /:/host alpine /bin/sh
    chroot /host
    ```
    Now you have access to all the files of the VM running dockerd. The log is at `/var/log/docker.log`.

### New user permissions may not be effective immediately after updating

When you grant new permissions (new roles) to a Service Principal, you may find out that the change is not effective immediately. There are two possible reasons:

* AAD role assignment delay. Normally it's fast; but it could take minutes due to propagation delay.
* Permission delay on ACR token server. This could take up to 10 minutes. To mitigate, user can do a logout and then login again with the same user after 1 minute:

     ```bash
    docker logout myregistry.azurecr.io
    docker login myregistry.azurecr.io
    ```

Currently ACR doesn't support home replication deletion by the users. The workaround is to include the home replication create in the template but skip its creation by using the **condition:false** as shown below

```
{
    "name": "[concat(parameters('acrName'), '/', parameters('location'))]",
    "condition": false,
    "type": "Microsoft.ContainerRegistry/registries/replications",
    "apiVersion": "2017-10-01",
    "location": "[parameters('location')]",
    "properties": {},
    "dependsOn": [
        "[concat('Microsoft.ContainerRegistry/registries/', parameters('acrName'))]"
     ]
},
```

### Authentication information is not given in the correct format on direct REST API calls

You may encounter an `InvalidAuthenticationInfo` error, especially using the `curl` tool with the option `-L`, `--location` (follow redirects).

For example, fetching the blob using `curl` with `-L` option and basic authentication.
```bash
curl -L -H "Authorization: basic $credential" https://$registry.azurecr.io/v2/$repository/blobs/$digest
```
may result in
```xml
<?xml version="1.0" encoding="utf-8"?>
<Error><Code>InvalidAuthenticationInfo</Code><Message>Authentication information is not given in the correct format. Check the value of Authorization header.
RequestId:00000000-0000-0000-0000-000000000000
Time:2019-01-01T00:00:00.0000000Z</Message></Error>
```

The root cause that some `curl` implementations follow redirects with headers from original request, which shoud not.
To resolve the problem, you need to follow redirects manually without the headers. It can be done using the `-D -` option of `curl`.
Here is the full script:
```bash
redirect_url=$(curl -s -D - -H "Authorization: basic $credential" https://$registry.azurecr.io/v2/$repository/blobs/$digest | grep "^Location: " | cut -d " " -f2 | tr -d '\r')
curl $redirect_url
```

## Tasks

### How to batch cancel runs?

The following commands cancel all running runs in the specified registry.

```bash
az acr task list-runs -r $myregistry --run-status Running --query '[].runId' -o tsv \
| xargs -I% az acr task cancel-run -r $myregistry --run-id %
```

### How to include .git folder in az acr build command?

If you pass a local source folder to `az acr build` command, `.git` folder is excluded from the uploaded package by default. You can create a `.dockerignore` file with the following setting. It will tell the command to include back all files under `.git` in the uploaded package. It also applies to `az acr run` command.

```
!.git/**
```
