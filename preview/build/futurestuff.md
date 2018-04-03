# Future Stuff

- View dependencies

```bash
az acr build-task show -n helloworld -r jengademos --query dependencies
```

- Configure base image updates

```bash
az acr build-task configure -n helloworld -r jengademos --base-image-updates runtime
```

- Make a change to the base image
  - Commit a code change that reflects a value that shows in the subsequent app
  - Commit
- Watch the logs for the automated base image build task

```bash
az acr build-task show-log -n helloworld -r corp-aspnetcore
```

- Watch the logs for the helloworld image, which is automatically built when the base image, corp-aspnetcore is completed.
```bash
az acr build-task show-log -n helloworld -r jengademos
```