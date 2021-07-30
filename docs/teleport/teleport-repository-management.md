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

### Manually enable teleport on a repository

### Manually disable teleport on a repository

### Check an image is ready to be teleported

## Planned Flow
We understand that the private preview has some frustrating limitations with the above flow and aspects that can be confusing, as a result there is a proposed flow for this:

