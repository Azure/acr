
## Setup Automated Build

> Automated builds are not yet avaialble in the az acr build CLI. They should be avaiable soon. To get a feel for the experience, and to provide us feedback, the following walkthrough is available:


- Create a build definition, which is automatically triggered on scc commits. 

> ACR Build currently supports github based tokens. VSTS tokens will come in a future preview

```bash
az acr build-task create --task helloworld -n jengademos \
    -t helloworld:{buildnumber} -f ./HelloWorld/Dockerfile \
    --cpu 2 --context https://[yourrepo] --git-access-token [gitHubToken]
```

- Trigger the build with a source change
  - code change
  - commit

- View the status

```bash
az acr build show-logs --task helloworld
```

- Manually trigger the build

```
az acr build --task helloworld
```

- View dependencies

```bash
az acr build definition show definition-name helloworld --query dependencies
```

- Configure base image updates

```bash
az acr build configure --definition-name --base-image-updates final
```

- Make a change to the base image
  - Commit a code change that reflects a value that shows in the subsequent app
  - Commit
- Watch the logs for the automated base image build definition

```bash
az acr build show-logs --task corp-aspnetcore
```

- Watch the logs for the helloworld image, which is automatically built when the base image, corp-aspnetcore is completed.
```bash
az acr build show-logs --task helloworld
```
