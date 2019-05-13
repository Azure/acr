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