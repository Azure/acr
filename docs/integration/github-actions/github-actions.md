# Using Azure Container Registry With GitHub Actions

For creating workflows for your GitHub repository using GitHub Actions, please refer [https://developer.github.com/actions/](https://developer.github.com/actions/).

The following `main.workflow` file defines a workflow that uses the built-in Docker Actions to login to the Azure Container Registry, build and push an image to the registry. You also needs to define three secrets to pass the registry access information to the Actions.

| Secret/Environment Variable | Description |
| --------------------|---------------------------------------------------------------|
| DOCKER_REGISTRY_URL | Login server url for the registry, eg, myregistry.azurecr.io  |
| DOCKER_USERNAME     | Service principal App ID or admin username for the registry   |
| DOCKER_PASSWORD     | Service principal password or admin password for the registry |

main.workflow
---
```
workflow "DockerFlowExample" {
  resolves = ["Docker Push"]
  on = "push"
}

action "Docker Login" {
  uses = "actions/docker/login@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  secrets = ["DOCKER_REGISTRY_URL", "DOCKER_USERNAME", "DOCKER_PASSWORD"]
}

action "Docker Build" {
  uses = "actions/docker/cli@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  needs = ["Docker Login"]
  args = ["build", "-t", "$DOCKER_REGISTRY_URL/hello-world:latest", "docs/integration/github-actions"]
  secrets = ["DOCKER_REGISTRY_URL"]
}

action "Docker Push" {
  uses = "actions/docker/cli@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  needs = ["Docker Build"]
  args = ["push", "$DOCKER_REGISTRY_URL/hello-world:latest"]
  secrets = ["DOCKER_REGISTRY_URL"]
}
```
