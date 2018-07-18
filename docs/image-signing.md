# Azure Container Registry Image Signing

Azure Container Registry supports image signing through [Docker Content Trust](https://docs.docker.com/notary/getting_started/).

To push signed images to ACR, the following configuration is required:
* The user or Service Principal used for automated signing must be assigned the `AcrImageSigner` role to your registry in addition to the `Owner`, `Contributor` roles for signing. Role assignment can be done by the following methods.
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

You can use Dokcer Client and Notary Client to interact trusted images with ACR.
Detailed documentation can be found at [Content trust in Docker](https://docs.docker.com/engine/security/trust/content_trust/).