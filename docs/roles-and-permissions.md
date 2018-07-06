# ACR Roles & Permissions
ACR supports a set of permissions, assigned to specific Azure Roles.
Using Azure IAM, specific permissions can be assigned to users and/or service principals.
The below table represents the Azure Roles and the ACR Permissions applied

| Role/Permission       | [ARM Access](#arm-access)| [Create/Delete ACR](#create/delete-acr) | [Push](#push) | [Pull](#pull) | [Policy Changes](#policy-changes) | [Change Quarantine State](#change-quarantine-state) | [Pull Quarantine Images](#pull-quarantine-images) | [Signature Signing](#signature-signing)  |
| ---------| --------- | --------- | --------- | --------- | --------- | --------- | --------- | ---------  |
| Owner | X | X | X | X | X |  |  |   |
| Contributor | X | X | X | X | X |  |  |   |
| Reader | X |  |  | X |  |  |  |   |
| AcrPush |  |  | X | X |  |  |  |   |
| AcrPull |  |  |  | X |  |  |  |   |
| AcrQuarantineWriter |  |  |  |  |  | X | X |   |
| AcrQuarantineReader |  |  |  |  |  |  | X |   |
| AcrImageSigner |  |  |  |  |  |  |  | X |

## Differentiating Users and Services

Anytime permissions are applied, best practices suggest providing the most limited set of permissions for a person, or service, to accomplish their task. The following permission sets represent a set of capabilities that may be used by humans and headless services.

### CI/CD Solutions
When automating `docker build`s from CI/CD solutions, you'll need `docker push` capabilities. For these headless service scenarios, we'd suggest assinging the **AcrPush** role. This limits the account from access through the portal. While we don't worry about code going rouge and doing additional destructive tasks, depending on how you limit the access keys, users may get the username/password credentials required to do damage.

### Container Host Nodes
Likewise, nodes running your containers will need the **AcrPull** role, but shouldn't require **reader** capabilities.

### Tools like the VS Code ACR extension
For tools like the VS Code ACR extension, additional resource provider access will be required to list the set of registries available. In this case, you would provide your users access to the **reader** and/or **contributor** role. These roles will allow `docker pull`, `docker push` and `az acr list`, `az acr build` and other capabilities. 


## ARM Access

ARM represents the Azure Resource Manager. ARM access is required for the Azure Portal and [az cli](https://docs.microsoft.com/en-us/cli/azure/). To get a list of registries, such as `az acr list`, you will need this permission set. 

## Create/Delete ACR

The ability to create and delete registries

## Push

The ability to `docker push` and image to the registry

## Pull

The ability to `docker pull` an image, that has not been quarantined, from the registry.

## Policy Changes

The ability to configure policies on the registry, such as image purging, enabling quarantine and image signing.

## Change Quarantine State

The ability to set the quarantine state of an image. This role should only be assigned to vulnerability scanners using service principals. Individual users, even operations people should use the vulnerability scanning solution to override the quarantine state.

## Pull Quarantine Images

The ability to `docker pull` images by their digest, allowing a vulnerability scan. 
!Note: This role should only be assigned to vulnerability scanners using service principals. Individual users, even operations people should use the vulnerability scanning solution to override the quarantine state.

## Signature Signing

The ability to sign images, usually assigned to an automated process, which would use service principals.
