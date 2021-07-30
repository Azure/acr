# Manage Repositories in Teleport Enabled Registries

## Existing Limitations
- Registries must first be Teleport enabled to enable repositories
- There is a current 10 teleport enabled repository for registries 

## Existing Flow

At the moment if a repository is teleport enabled, this means it can expand images that are pushed into it making them into the teleport format. Note that this does not interfere with regular registry storage and teleportable (expanded) layers are stored in a separate  storage than typical layers. Making a repository enabled does not however expand all existing images in it, rather all images pushed after the fact will be expanded. 

This can be best illustrated with examples, take a new empty registry that has already been teleport enabled, in this case we can summarize its state as:

    Registry A
        Properties
        - Teleport enabled
        -> Repositories 
            (none)
        

Pushing any image to Registry A will result in the creation of a teleport enabled repository, for visualization:

    Push ubuntu:18.0.1 to Registry A

    Registry A
        Properties
        - Teleport enabled
        -> Repositories 
            ubuntu (Teleport Enabled)
                |-> Tag: 18.0.1 
                    Can be pulled from teleport client


Now consider a non empty registry, registry B that has just had teleport enabled

    Registry B
        Properties
        - Teleport enabled
        -> Repositories 
            python (Not Teleport Enabled)
                |-> Tag: latest
                    Cannot be pulled from teleport client

Pushing a new image not already present will result in:

    Push ubuntu:18.0.1 to Registry B

    Registry B
        Properties
        - Teleport enabled
        -> Repositories 
            python (Not Teleport Enabled)
                |-> Tag: latest
                    Cannot be pulled from teleport client

            ubuntu (Teleport Enabled)
                |-> Tag: 18.0.1 
                    Can be pulled from teleport client

If we want to enable python we can similarly re push the python image resulting in:

    Push python:latest to Registry B

    Registry B
        Properties
        - Teleport enabled
        -> Repositories 
            python (Teleport Enabled)
                |-> Tag: latest
                    Can be pulled from teleport client

            ubuntu (Teleport Enabled)
                |-> Tag: 18.0.1 
                    Can be pulled from teleport client

A tricky thing however is that if instead of the last step we pushed a different image for repository python (where latest and 2.2 dont share a digest) we would see this state:


    Push python:2.2 to Registry B

    Registry B
        Properties
        - Teleport enabled
        -> Repositories 
            python (Teleport Enabled)
                |-> Tag: latest
                    Cannot be pulled from teleport client
                    Tag: 2.2
                    Can be pulled from teleport client

            ubuntu (Teleport Enabled)
                |-> Tag: 18.0.1 
                    Can be pulled from teleport client

 
## Manually Select which Repositories Are Teleport Enabled

The previous operation does not give customers much control over which registries are teleport enabled as a result we do have an existing flow to chose which repositories are enabled and which arent, note however that as long as a repository is under the 10 repo limit all pushes to non teleport enabled repositories (new or otherwise) will become teleport enabled (even if the flag here was set manually to disabled)

The previous step should have provided some insights as to how we currently enable teleport on a repository. Here is how to explicitely set and check which repositories are teleport enabled.

### Identify which repositories are teleport enabled

To currently identify if a speific repository is teleport enabled we can run the following command and looking at the teleport enabled field:

```bash
az acr repository show --repository <repository>  -o jsonc
{
"changeableAttributes": {
  "deleteEnabled": true,
  "listEnabled": true,
  "readEnabled": true,
  "teleportEnabled": true,
  "writeEnabled": true
 }
}
```

We have also provided a convenicence script to find this out for all repositories in a registry in case there are a lot and determining this becomes difficult. P.S this is not super fast but saves manually checking one by one.

> Note: Assure [find-teleport-enabled-repositories.sh](./find-teleport-enabled-repositories.sh) is set to execute: `sudo chmod +x find-teleport-enabled-repositories.sh`

You can run this as follows:

```bash
./find-teleport-enabled-repositories.sh registry-name
```

Sample output:
```bash
/find-teleport-enabled-repositories.sh teleporttest
gcc -> Enabled
glassfish -> Enabled
jupyter/all-spark-notebook -> Enabled
python -> Disabled
rethinkdb -> Enabled

Summary:
Enabled Repositories:

gcc
glassfish
jupyter/all-spark-notebook
rethinkdb

```




### Manually disable teleport on a repository

For the time being we disable teleport on a repository using the [edit-teleport-attribute.sh](./edit-teleport-attribute.sh)  included in this repo. This can be used by first setting env variables for credentials:

> Note: Assure [edit-teleport-attribute.sh](./edit-teleport-attribute.sh) is set to execute: `sudo chmod +x edit-teleport-attribute.sh`

```bash
export ACR_USER=teleport-token
export ACR_PWD=$(az acr token create \
  --name teleport-token \
  --registry $ACR \
  --scope-map _repositories_pull \
  --query credentials.passwords[0].value -o tsv)

edit-teleport-attribute.sh <registry-name> <repository> disable --debug
```

### Manually enable teleport on a repository

Once the registry has less than 10 teleportable repositories enabled, the next repository for which an image is pushed that is not already teleport enabled will become teleport enabled. As a result there is no direct need of enabling teleport for it, instead you can just push an image to said repository. 

Nonetheless for completeness [edit-teleport-attribute.sh](./edit-teleport-attribute.sh)  script can set this metadata field to enable teleport on a repository manually (you will still need to push afterwards so the image will expand in the background).

```bash
export ACR_USER=teleport-token
export ACR_PWD=$(az acr token create \
  --name teleport-token \
  --registry $ACR \
  --scope-map _repositories_pull \
  --query credentials.passwords[0].value -o tsv)

edit-teleport-attribute.sh <registry-name> <repository> enable --debug
```

After, new images pushed to the enabled repository will be expanded and teleportable.

## FAQ

 - Can the Teleport 10 repository limit be raised?

    ``` The 10 repository limit is a temporary measure that we have included due to cost constraints during the private preview phase. We do not currently have a way to raise this value for a particular registry. If necessary users can request a second registry to be teleport enabled ```

 - Will pushing one image to an existing repository (making it teleport enabled) expand all existing tags making them teleportable?

     ``` Unfortnately we do not currently support backfill so only layers contained in pushes after teleport has been enabled on the registry will be fixed ```

 - Is there a timeline to improve this behaviour?

   ``` We have a plan to improve this behaviour but the exact timeline is not set in stone ```

 - How can I tel if an image in a teleport enabled repository is actually teleportable?

   ``` This can be done using the check-expansion script also provided in this registry ```


