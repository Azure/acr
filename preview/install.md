# ACR Preview CLI Installation Steps

The `az acr build` CLI is in preview, and must be installed manually.

- [Install the Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-macos?view=azure-cli-latest)

- After installing the az cli, remove any previous versions of `acr build` and install the current extension, using the following commands:

```
az extension remove -n acrbuildext
az extension add --source https://acrbuild.blob.core.windows.net/cli/acrbuildext-0.0.2-py2.py3-none-any.whl -y
az acr build --help
```

## Next Steps
- [Quick Start - az acr build through cloud shell](./build/readme.md)
- [Quick Start - create a build with az acr build, test with ACI](./build/quickstart-acrbuild.md)
- [Quick Start - create a build-task, triggered by git commits, and view logs](./build/quickstart-buildtask.md)
