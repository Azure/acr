# Build Enhancements in ACR Tasks

Building images using [buildx](<https://github.com/docker/buildx>) and [buildkit](<https://github.com/moby/buildkit>) is supported by [ACR Tasks](<https://aka.ms/acr/tasks>).

With `buildx`, build performance is enhanced with various advanced features, such as concurrent building and cache import/export support.

The overall performance comparison is presented as below while the underlying ACR tasks are run in the south-east Asia region.

| Image                                                        | build  | buildx | buildx and initialize cache | buildx with cache |
| ------------------------------------------------------------ | ------ | ------ | --------------------------- | ----------------- |
| [oras](<https://github.com/deislabs/oras>)                   | 2m48s  | 2m15s  | 3m0s                        | 25s               |
| [moby](<https://github.com/moby/moby>)                       | 15m34s | 9m50s  | 12m40s                      | 1m45s             |
| [docker-stacks/all-spark-notebook](<https://github.com/jupyter/docker-stacks/tree/master/all-spark-notebook>) | 12m52s | 8m47s  | 10m0s                       | 2m50s             |
| [azure-cli](<https://github.com/Azure/azure-cli>)            | 7m33s  | 5m59s  | 6m1s                        | 1m15s             |
| [nodejs-docker-example](<https://github.com/buildkite/nodejs-docker-example>) | 1m59s  | 1m18s  | 1m14s                       | 52s               |

As shown above, `docker buildx` is generally faster than `docker build` since `buildx` builds images concurrently with multi-stage Dockerfiles. To [build with cache](#build-with-cache), the first run of `buildx` is expected to be slower since there is no cache existing and it requires extra time to export caches. The subsequent run is expected to be faster, utilizing the existing caches.

## Run `buildx` in ACR Tasks

Since `buildx` has not been integrated with ACR Tasks, it is required to build `buildx` from its source before actually using it. The `buildx` image can be built by ACR Tasks using the multi-step task YAML file [bootstrap.yaml](bootstrap.yaml) as follows.

```sh
az acr run -r myregistry -f bootstrap.yaml /dev/null
```

The resulted `buildx` image will be pushed to `myregistry.azurecr.io/buildx`. Visually, running the `buildx` image is equivalent to run the `docker buildx` command.

### Build using `buildx`

Images can be built using `buildx`. An example multi-step task YAML file [build.yaml](build.yaml) is provided and can be run as follows.

```sh
az acr run -r myregistry -f build.yaml \
    --set URL=https://github.com/myuser/myrepo.git \
    --set NAME=myrepo \
    /dev/null
```

The resulted image will be pushed to `myregistry.azurecr.io/myrepo`.

For instance, run the following task to build `oras` and push to `myregistry.azurecr.io/oras` using `buildx`.

```sh
az acr run -r myregistry -f build.yaml \
    --set URL=https://github.com/deislabs/oras.git \
    --set NAME=oras \
    /dev/null
```

It is also possible to build local repository using `buildx`. Run the following task to build using `buildx` with the context path `local-repository-folder-path`.

```sh
az acr run -r myregistry -f build.yaml --set URL=. --set NAME=myrepo local-repository-folder-path
```

### Build with Cache

Building progress can be speeded up using caches. An example multi-step task YAML file [build_with_cache.yaml](build_with_cache.yaml) is provided and configured to export max cache. It can be run as follows.

```sh
az acr run -r myregistry -f build_with_cache.yaml \
    --set URL=https://github.com/myuser/myrepo.git \
    --set NAME=myrepo \
    /dev/null
```

The resulted image will be pushed to `myregistry.azurecr.io/myrepo`, and the cache is imported from / exported to `myregistry.azurecr.io/myrepo:cache`.

The first run of the building process is expected to be slower than a normal `buildx` build since it has no cache imported and it requires extra time to export the resulted cache. The subsequent runs are expected to be faster as the valid cache is imported.

