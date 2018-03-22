# Integrating ACR Build into Build Pipelines

ACR Build is a native container build service. ACR Build is not intended to replace or compete with other build systems. It's a core primitive that may be used stand alone, or integrated into existing build pipelines.

## Integration Options

ACR Build supports multi-stage dockerfiles, and you can bring your own build environment (BYOE). So, there's not much you can't build in ACR Build. 

## Using ACR Build for CI, Using ___ for Release

The typical result of an ACR Build results in an image in the designated registry. When images arrive in ACR, a region specific image-pushed [webhook](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-webhook) fires. This may be enough for you to trigger your release management solution.

### ACR Build Triggers

ACR Build can be triggered from several sources:

- git commits
- base image updates
- additional webhook triggers, including Azure Event Grid
- manually triggered with the az cli `az acr build --task [taskname]`

## Integrating ACR Build into ___ Build Systems
*Comming soon*
### Integrating ACR Build with VSTS
*Comming soon*
### Integrating ACR Build with Jenkins
*Comming soon*
### Integrating ACR Build with Brigade
*Comming soon*

