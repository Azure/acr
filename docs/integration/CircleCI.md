# Using Azure Container Registry With CircleCI

For configuration of your Docker build using CircleCI, refer [https://circleci.com/docs/1.0/docker/](https://circleci.com/docs/1.0/docker/)

Here is a sample `circle.yml` file that can be used with Azure Container Registry using three environment variables as a part of the build, that builds and pushes an image to the registry.

``` yml
machine:
  services:
    - docker

dependencies:
  override:
    - docker info
    - docker build --rm=false -t $REGISTRY_HOST/circleci .

test:
  override:
    - docker run -d hello-world

deployment:
  hub:
    branch: master
    commands:
      - docker login -e $DOCKER_USER -u $DOCKER_USER -p $DOCKER_PASSWORD $REGISTRY_HOST
      - docker push $REGISTRY_HOST/circleci
```

| Environment Variable | Description |
| --------------------|-------------|
| REGISTRY_HOST       | Login server host for your Registry |
| DOCKER_USER         | Service principal or admin user for the registry |
| DOCKER_PASSWORD     | User's password that would be used for docker login |
