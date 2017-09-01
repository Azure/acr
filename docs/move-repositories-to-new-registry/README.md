# How to move your repositories to a new registry?

When users create a container registry backed by a storage account, the repositories are pushed under a blob container that is named after the registry within that storage account. 

In the example below we have two registries in a resource group associated with the same storage account. 

![alt Registries](move-repositories-to-new-registry-sourceregistry.png)

Here  using [Azure Storage Explorer](http://storageexplorer.com/) we can see that each registry gets a container with the corresponding registry name. 

![alt Registries container data](move-repositories-to-new-registry-sourceregistry-data.png)

All you need to do is move the blobs from one container to the other if you want to copy over the repositories. If you do not care about the old container registry then you can just rename the blob container and delete the registry since deleting a registry does not delete the associate data in your storage account. 

![alt Copy blogs](move-repositories-to-new-registry-sourceregistry-copy.png)


> Make sure you paste that into the target registry's blob container and you should be able to pull your images from the new registry.