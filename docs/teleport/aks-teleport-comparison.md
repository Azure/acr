---
title: Comparing Azure Container Registry Project Teleport with standard docker pull, using Azure Kubernetes Service
description: Perform A/B comparison of the same image across two nodes in an AKS Cluster. One with Project Teleport enabled, one without.
services: container-service
ms.topic: article
ms.date: 02/26/2021
---

# Comparing Azure Container Registry Project Teleport with standard docker pull, using Azure Kubernetes Service

> Note: this is a first draft, to get folks started.

To get a sense of the performance benefits of Project Teleport two deployments will be made allowing the same image to be deployed to an AKS node with project teleport, and another without Project Teleport enabled.

Project Teleport is node specific. If an image is pulled to a node, with teleport enabled, the expanded layers are mounted.
If a second copy of the same image is pulled to the same node, even if pulled from a non-teleport expanded repository, the node will identify common layers and mount the layers from the previously pulled and teleport expanded image.

To avoid layer sharing, testing the same image with and without Project Teleport enabled, two additional nodepools will be created. The additional nodepools will enable clearing any cached images and layers by scaling the nodepool to zero, then back to one.

When complete, the AKS cluster will have (3) nodepools:

- `nodepool1` - The system nodepool. No workloads will be scheduled here.
- `teleporter` - A teleport enabled nodepool, with a single node
- `shuttle` - The standard method of transport of container images, with a single node.

This tutorial assumes you've already completed the steps to create a Teleport enabled AKS Cluster, and Teleport enabled ACR Instance. If you haven't already done so, complete the steps in: [Integrate Azure Container Registry and Project Teleport with Azure Kubernetes Service](./aks-getting-started.md)

## Set environment variables

Configure variables unique to your environment. Note the ACR and AKS instances must both be in one of the [teleport supported regions][teleport-regions].

```azurecli-interactive
AKS=myaks
AKS_RG=${AKS}-rg
RG_LOCATION=westus2
K8S_VERSION=1.19.7
ACR=myacr
ACR_URL=${ACR}.azurecr.io
```

## Clone the Teleport samples repo

Sample kubernetes deployment files and a `check-expansion.sh` script are provided at: https://github.com/Azure/acr/tree/main/docs/teleport. Only a few files are necessary. You may either `git clone` the repo, or copy the individual files referenced below.

```bash
git clone https://github.com/Azure/acr.git
```

## Import images for teleportation

For completeness of this walkthrough, the azure-vote application is used, which includes a 944mb `azure-vote-front:v1` image. To expand the layers, import the images into a teleport enabled registry, in the same region as the AKS cluster.

```azurecli-interactive
az acr import \
  --source mcr.microsoft.com/azuredocs/azure-vote-front:v1 \
  --name $ACR \
  --image azure-vote-front:v1

az acr import \
  --source mcr.microsoft.com/oss/bitnami/redis:6.0.8 \
  --name $ACR \
  --image redis:6.0.8
```

## Confirm import and teleport expansion

Confirm the `azure-vote-front` repository is set for teleport expansion:

```azurecli
az acr repository show \
  --repository azure-vote-front \
  -o jsonc
```

Look for `"teleportEnabled": true,` in the output

```json
"changeableAttributes": {
  "deleteEnabled": true,
  "listEnabled": true,
  "readEnabled": true,
  "teleportEnabled": true,
  "writeEnabled": true
}
```

Although the repository is configured for teleport expansion, each image upload will take time to be expanded on push. The length of time is based on the quantity and size of layers, however the expansion should be completed within seconds.

> **Note:** ACR webhooks indicate when an artifact is pushed and available. A new webhook notification will be added at a future date to indicate the image has been expanded, and ready for teleportation.

At this point in the Teleport preview, check expansion using the `check-expansion.sh` script. As the script uses a `/mount` api, basic auth is required. An [ACR Token](https://aka.ms/acr/tokens) is created and saved as environment variable.

```azurecli-interactive
export ACR_USER=teleport-token
export ACR_PWD=$(az acr token create \
  --name teleport-token \
  --registry $ACR \
  --scope-map _repositories_pull \
  --query credentials.passwords[0].value -o tsv)

./check-expansion.sh teleport azure-vote-front v1
```

## Add nodes for teleporters and shuttles

Two nodepools will be added to enable teleportation, and a comparison for standard transport. An `acr-teleport` label is added for scheduling onto specific nodes. The system nodepool (`nodepool1`) is avoided to enable [clearing the image cache](#cleanup) by scaling the nodepool to zero then back to one.

```azurecli-interactive
az aks nodepool add \
  --resource-group $AKS_RG \
  --cluster-name $AKS \
  --name teleporter \
  --node-count 1 \
  --aks-custom-headers EnableACRTeleport=true \
  --labels acr-teleport=enabled

az aks nodepool add \
  --resource-group $AKS_RG \
  --cluster-name $AKS \
  --name shuttle \
  --node-count 1 \
  --labels acr-teleport=disabled
```

## Deploy to AKS

Update the `azure-vote-teleport.yaml` and `azure-vote-shuttle.yaml` files to reference your registry name:

```yml
spec:
  ...
  containers:
  - name: azure-vote-back
    image: <registryName>.azurecr.io/redis:6.0.8
  ...
  containers:
  - name: azure-vote-front
    image: <registryName>.azurecr.io/azure-vote-front:v1
```

### Deploy with standard pull performance

Deploy the _shuttle_ podspec:

```azurecli-interactive
kubectl apply -f azure-vote-shuttle.yaml
```

Get the list of pods to find the azure-vote-front pod. The shorthand version can be used if only one pod is named `azure-vote-front`. You may need to run the command a few times until the image has been pulled and expanded on the node.

```azurecli-interactive
kubectl get pods
kubectl describe pod azure-vote-front
```

Under the `events` list, an entry for `Successfully pulled image...` provides the pull time. Note the length before proceeding to the teleport version.

```bash
Events:
  Type    Reason     Age   From               Message
  ----    ------     ----  ----               -------
  Normal  Scheduled  36s   default-scheduler  Successfully assigned default/azure-vote-front-5c976dbbd9-tckdz to aks-shuttle-10583637-vmss000000
  Normal  Pulling    35s   kubelet            Pulling image "teleport.azurecr.io/azure-vote-front:v1"
  Normal  Pulled     1s    kubelet            Successfully pulled image "teleport.azurecr.io/azure-vote-front:v1" in 34.738162s
```

If `already present on machine` is returned, this indicates the image was previously pulled and cached. See [recycle nodepool](#cleanup) to clear the cache.

```bash
Events:
  Type    Reason     Age    From               Message
  ----    ------     ----   ----               -------
  Normal  Scheduled  2m12s  default-scheduler  Successfully assigned default/azure-vote-front-5bdfc85f9c-d7z8b to aks-shuttle-10583637-vmss000000
  Normal  Pulled     2m11s  kubelet            Container image "teleport.azurecr.io/azure-vote-front:v1" already present on machine
```

### Deploy with Teleport performance

Deploy the _teleport_ podspec:

```azurecli-interactive
kubectl apply -f azure-vote-teleport.yaml
```

Get the list of pods to find the azure-vote-front pod. The shorthand version can be used if only one pod is named `azure-vote-front`. You may need to run the command a few times until the image has been pulled and expanded on the node.

```azurecli-interactive
kubectl describe pod azure-vote-front-teleport
```

Under the `events` list, an entry for `Successfully pulled image...` provides the pull time. Note the length should be dramatically faster.

```bash
Events:
  Type    Reason     Age   From               Message
  ----    ------     ----  ----               -------
  Normal  Scheduled  12s   default-scheduler  Successfully assigned default/azure-vote-front-teleport-5bf865d976-lm4bj to aks-teleporter-10583637-vmss000000
  Normal  Pulling    11s   kubelet            Pulling image "teleport.azurecr.io/azure-vote-front:v1"
  Normal  Pulled     3s    kubelet            Successfully pulled image "teleport.azurecr.io/azure-vote-front:v1" in 8.011045191s
  Normal  Created    3s    kubelet            Created container azure-vote-front-teleport
  Normal  Started    3s    kubelet            Started container azure-vote-front-teleport
```

### Cleanup

To reset the nodes, delete the two deployments:

```azurecli-interactive
kubectl delete -f azure-vote-teleport.yaml
kubectl delete -f azure-vote-shuttle.yaml
```

Clear the image cache, and any teleport mounts:

```azurecli-interactive
az aks scale \
  --resource-group $AKS_RG \
  --name $AKS \
  --nodepool-name teleporter \
  --node-count 0

az aks scale \
  --resource-group $AKS_RG \
  --name $AKS \
  --nodepool-name shuttle \
  --node-count 0

az aks scale \
  --resource-group $AKS_RG \
  --name $AKS \
  --nodepool-name teleporter \
  --node-count 1

az aks scale \
  --resource-group $AKS_RG \
  --name $AKS \
  --nodepool-name shuttle \
  --node-count 1
```

## Performance profile of teleport

The Voting app has a significant performance delta, comparing a normal pull time of 34.7 seconds, with 8.0 seconds of teleport. While impressive, you may have assumed a larger difference.

Teleport prototype-1 is based on mounting expanded layers. For each layer of an image, the decompression of a layer is traded off for a mount. Therefore, the larger the layer count, the slightly longer the start time.

The `azure-vote-front` image has ***29 layers***, which requires 29 mounts. When pulling the image without teleport, the 944mb of content must be decompressed, but multiple decompression threads can run concurrently.

```bash
docker inspect teleport.azurecr.io/azure-vote-front:v1
...
"RootFS": {
  "Type": "layers",
  "Layers": [
      "sha256:e27a10675c5656bafb7bfa9e4631e871499af0a5ddfda3cebc0ac401dfe19382",
      "sha256:851f3e348c69d8959d326f0bab975c03f9813eec33aba389aa7c569953510433",
      "sha256:06f4de5fefeae30802d336e8c234b9c0989542fb80efd4f83be06c41aba26d9f",
      "sha256:b31411566900643c38169980a21093c23e0a12a12ffea78b1921d07dd40372bd",
      "sha256:6662dddae6aa455371366ed12400556a29e049373ea27c089a24634e3098cb48",
      "sha256:4ea12feed6a9386d7bdac8b26073b1209f0f39781a5d157026dfb5a918c95db7",
      "sha256:e7cee6196d865755606c73b82004784273cd423217cba8faf650b6707d3b5059",
      "sha256:b15d32f8b6aa975b8be84e825952094d2f20296777a2bb5fad3fb270ca05a776",
      "sha256:5e8efc7c6f4fee7fecfc685b742293a5300cc3180262a144a2fed54c46597129",
      "sha256:4f6e0a34a0535f5cd6b76d06aae861c3ced179b3b115d2850af0e2f0bcefeffc",
      "sha256:5ac43729c58be5ecb0fc13b164fb4f06f0afc13394735e8ac10cdb0f75311195",
      "sha256:491bd929c5bfcb0639a6c43d07be0aac225dc0da28379e99f617480599825e5f",
      "sha256:b18e79d2742360b7b0d81493e8a8beced51953c8a8f73fe4b228e47e8aeb292b",
      "sha256:55ebcfb2ad17cafdada768b6ca43e3f4e51bc589757b22337b94a499354aa052",
      "sha256:1a350e9420b7eac6b50172334afd6354d89749c62822951596bac9085cb9fb1e",
      "sha256:7b3929993879466a3abee028d3fb490d83c211ab5723e29765ed17c98db5b4e3",
      "sha256:ddeb470c209923815a410d35dd45a6710bc285955b5ef30d92a003d38bd68f3b",
      "sha256:c30da5f5d23cec0997d05337dd1113872ba56b38a59bf96922572f07d65b94a0",
      "sha256:a7364327f2826e4991e3675271350c9e7b858e33abbe77aacfbbff00a4b59455",
      "sha256:f8af872e501840a2de13260830960f612560d9ee755ae07a37c30758e8568444",
      "sha256:57fe04427c69233864b729d60d3c9c7fe8a43e950cb442a650101a357998c8c2",
      "sha256:cde1a4e95d8bea636972733fde8f223d1dda2c2425ec7de8ca5b078391723c11",
      "sha256:efa870440d9c6defc6447ad9d3d214312ba3dc0c665c723b793d14d241d811e1",
      "sha256:a9f64da753644ba8e18846cb23010c8e730de34701b5f519591167722a89784b",
      "sha256:2131d41261d2d13cae8b024c5a20e65fe8ee8f98d04bfd238124210b94115d69",
      "sha256:9d93163e41ffdad4659db82f267091b4a478f1235be1b25438407e79e80ed28b",
      "sha256:d9aeb057eef2070b1260cceeefb0933755f62504cf34efe2bf4f113043bf7493",
      "sha256:5e85a99d34e4a9aea5bdc845fb30587b172393ebf7d71ddbb1b325e3fa728090",
      "sha256:ab48c9fa73df063cfafaa0338c06ec44ba3d29a3ce6adde3fedf42d2d0c0ee91"
  ]
```

### Comparing fewer layers

To gauge the difference between layers and size, clone the [Azure-Samples/azure-voting-app-redis](https://github.com/Azure-Samples/azure-voting-app-redis) repo and build the image with the `--squash` flag.

```bash
docker build --force-rm --squash -t ${ACR}.azurecr.io/azure-vote-front:squashed .
docker push ${ACR}.azurecr.io/azure-vote-front:squashed
```

- Change the `:tag` references in `azure-vote-shuttle.yaml` and `azure-vote-teleport.yaml` files to `:squashed`.
- Follow the steps above to [recycle](#cleanup) the nodes, and [redeploy the two apps](#deploy-with-standard-pull-performance).

The resulting times should reflect **31.7 seconds** for the standard docker pull/decompress and **1.9 seconds** for teleportation of a single layer.

| Size | Layers | Docker |  Teleport|
|-|-|-|-|
| 944mb | 28 | 34.7 | 8 |
| 929mb | 1 | 31.7 | 1.9 |

That's a _further_ reduction from 8.0 seconds for 28 layers to 1.9 seconds for a single layer. This highlights the performance profile of mounting expanded layers, compared to pulling and decompressing layers.

#### Shuttle deployed single layer image

```
Events:
  Type    Reason     Age    From               Message
  ----    ------     ----   ----               -------
  Normal  Scheduled  9m55s  default-scheduler  Successfully assigned default/azure-vote-front-6ff785596-4d62g to aks-shuttle-10583637-vmss000000
  Normal  Pulling    9m54s  kubelet            Pulling image "teleport.azurecr.io/azure-vote-front:squashed"
  Normal  Pulled     9m22s  kubelet            Successfully pulled image "teleport.azurecr.io/azure-vote-front:squashed" in 31.711601788s
```

#### Teleported single layer image

```
Events:
  Type    Reason     Age   From               Message
  ----    ------     ----  ----               -------
  Normal  Scheduled  37s   default-scheduler  Successfully assigned default/azure-vote-front-teleport-5fbc9b754f-lrtpw to aks-teleporter-10583637-vmss000000
  Normal  Pulling    36s   kubelet            Pulling image "teleport.azurecr.io/azure-vote-front:squashed"
  Normal  Pulled     34s   kubelet            Successfully pulled image "teleport.azurecr.io/azure-vote-front:squashed" in 1.891985507s
```

### Balancing layers and size

While you might consider flattening your images to one layer for fast mounting, you may have contention on a single mount point. The purpose of the Teleport preview is to get further metrics on the usage to understand the art and science of image layers. The Teleport design does not require an image owner to make changes to use Teleport. Teleport works with your existing container images. However, with each technology, there are always optimizations that may be made based on the deployment target.

One thing is always common about image performance. The smaller you can make your overall container image, the faster it will run.

## Providing feedback

Please contact the Project Teleport technicians, and other fellow Teleport Red-Shirts in the [Teleport Red-Shirts teams channel][acr-teleport-red-shirts]

[acr-teleport-red-shirts]: https://aka.ms/acr/teleport/red-shirts
[teleport-regions]:     ./README.md#preview-constraints
