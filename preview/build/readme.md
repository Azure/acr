# ACR Build Preview information

## Announcements
We're happy to release Preview 4 which includes:

- build-task: a build definition that can be triggered on git commits, or run directly
with base image updates... 
- build-task logs: which lists the logs, their status and can connect to currently running build
- Tags now support {{.Build.Id}} to tag your newly built image with a unique tag. For more info: [Docker Tagging: Best practices for tagging and versioning docker images](https://blogs.msdn.microsoft.com/stevelasker/2018/03/01/docker-tagging-best-practices-for-tagging-and-versioning-docker-images/)
- Perf improvements - we've imporved the streamling log scenarios that have had a big impact on the overal build time. This turned out to be more of a bug, and isn't the perf enhancements we'll enable *soon*. 

## Getting access to ACR Build Preview

Request early access to ACR Build at: https://aka.ms/acr/preview/signup


> For Preview 4, ACR Build is currently available in EastUS. 

## Providing Feedback

We're currently in private preview, which is why our docs are hosted here. 
To discuss ACR Build with the product team and others within the preview, once you're signed up and given access, you can find us at: http://azurecr.slack.com/

## Try ACR with Cloud Shell
[acr build quickstart](./quickstart-acrbuild.md)

## Next Steps
Walk through the preview docs, starting with [acr build quickstart](./quickstart-acrbuild.md)
Provide us feedback and help us improve container lifecycle management...
