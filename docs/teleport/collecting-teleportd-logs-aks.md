# Collecting Logs for Teleport on AKS
This guide goes over how to collect logs for the teleportd daemon running in an AKS cluster. The steps in this guide have to be carried out by customers in order to collect log information for debugging purposes. 

## Teleportd logs
We can get teleportd logs from each node independently to verify that a teleport enabled image succesfully teleported. Because Teleportd is run as a daemon in the node, its logs are only available from journald in a node. To get these a user can either connect to their node which ran the specific pod being debugged (using ssh or lens) and run `journalctl -n all -u teleportd`. Alternatively you can collect logs by creating sidecar container on the corresponding nodes like we do. The sidecar mounts the filesystem of the nodepool and uses this to obtain journald logs. These are created using the following configuration:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: teleport-logs
spec:
  containers:
  - name: log-reader
    image: busybox
    args: [/bin/sh, -c, '/bin/journalctl -n all -u teleportd -f']
    volumeMounts:
    - name: rootfs
      mountPath: /
# Add if customer needs to specify node
#   nodeSelector:
#     teleport: "true" 
  volumes:
  - name: rootfs
    hostPath:
      path: /
      type: Directory
```

You can run a pod with the above configuration (make sure to edit the nodeSelector field to set the log collection to the specific node that needs its instance of teleportd to be debugged) and then get the logs from it by calling:
`kubectl logs teleport-logs > ./teleport-daemon.log` 


## Other options
### Kubernetes Events (Aside)
If the affected pod was just ran (events have a short timespan) you can use the following to gather some extra information and even confirm the image teleported:

For event collection we are looking at two methods:
- Running `kubectl describe pod <your pod name>`
    Teleportd events try to associate with the pod that first pulled a specific image within a node, nonetheless this can fail, in such a scenario events are not associated with a node but are still reported as general events and will still be visible when running kubectl get events. If everything goes rights the output of this command will include the teleport events, otherwise:

- Running `kubectl get events`
    In cases when teleportd fails to associate events with a pod or when multiple pods experienced issues, check all the kubectl get events, they are all sourced from the teleportd client and are marked as such, some will have the image and tag that was teleport or if it failed to teleport.

Events include overall failure information for teleportd but do not currently give information on individual layer failures. If an image took too long to teleport for example and the events still indicate success there could be individual layer mount failures, refer to the logs in that scenario.
