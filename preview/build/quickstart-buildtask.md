# Setup Automated Build

ACR Build can be triggered on:
- git commits
- base image updates *
- Webhooks *
- Azure Evengtrid notifications *
    
    \* indicates events not yet supported

> ACR Build currently supports github based PAT tokens. VSTS tokens will come in a future preview

## Create a Github Personal Access Token
- Create a github token by navigating to: 
    https://github.com/settings/tokens/new
- Under repo, enable repo:status, public_repo

    ![](./media/CreateGithubToken.png)

- Copy the generated token

## Create a build task, which is automatically triggered on scc commits. 

With the git PAT, execute the following command replacing the context with your github:

```bash
az acr build-task create --name helloworld -n jengademos \
    -t helloworld:v1 -f ./HelloWorld/Dockerfile \
    --context https://github.com/SteveLasker/aspnetcore-helloworld --git-access-token [yourToken]
```

> Note: Setting the :tag to :{build.Id} will be implemented in a future preview

### Specifying a sub folder

We're exploring the same convention as the [docker cli](https://docs.docker.com/engine/reference/commandline/build/#git-repositories), to specify the branch and sub folder as well.

```bash
az acr build-task create --name helloworld -n jengademos \
    -t helloworld:v1 -f ./HelloWorld/Dockerfile \
    --context https://github.com/SteveLasker/aspnetcore-helloworld.git$subBranch:subFolder --git-access-token [yourToken]
```
Your feedback on [azurecr.slack.com](https://azurecr.slack.com) would be helpful...


# View the status
Build logs, including access to live streaming of current builds are available through the `build-task logs` parameter


### list the build tasks for a registry
```bash
az acr build-task list -r jengademos
```

### list the builds for a registry
```bash
az acr build-task list-builds -r jengademos
```

### list the builds for a build-task within a registry
```bash
az acr build-task list-builds -n hellowworld -r jengademos
```

### show the last (or current) log for a build-task
```bash
az acr build-task logs -n helloworld -r jengademos
```

### show the log for a specific build
```bash
az acr build-task logs --build-id eus-1 -r jengademos
```


```bash
az acr build-task logs -n helloworld -r jengademos
```

- Manually trigger the build

```
az acr build -n helloworld -r jengademos
```
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
