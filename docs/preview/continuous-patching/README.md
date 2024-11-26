Continuous Patching Workflow in Azure Container Registry
========================================================

## Introduction

Continuous Patching is a feature in Azure Container Registry that allows you to recurringly scan and patch specified artifacts for only OS-level  vulnerabilities within the registry. The workflow enables to set a cadence for run and create a recurring ACR task that scans your configured list of images for vulnerabilities (CVEs) using [Trivy](https://trivy.dev/) and patch them using [Copa](https://project-copacetic.github.io/copacetic/website/).
 

> **NOTE:**
> Continuous Patching is a limited pilot program as of October 2024.


## Use Cases

Here are a few scenarios to use Continuous Patching:

- **Enforcing container security and hygiene:** Continuous Patching enables users to quickly fix OS container CVEs without the need to fully rebuild from upstream.
- **Speed of Use:** Continuous Patching removes the dependency on upstream updates for specific images by updating packages automatically. Vulnerabilities can appear every day, while popular image publishers like Ubuntu only offer a new release once a month. With Continuous Patching, one can ensure that their image is OS vulnerability-free more frequently.

## Preview Limitations

Continuous Patching is currently in preview. The following limitations apply:
- Windows-based container images aren’t supported
- Only "OS-level" vulnerabilities will be patched. This includes packages in the image managed by a package manager such as “apt” and “yum”. Vulnerabilities at the “application level” are unable to be patched, such as compiled languages like Go, Python, NodeJS.    

## Prerequisites        

- You can use the Azure Cloud Shell or a local installation of the Azure CLI with a minimum version of 2.15.0 or later. 
- You have an existing Resource Group with an Azure Container Registry.

## Installing the Continuous Patching Workflow

Download the [wheel file](https://acrcssc.z5.web.core.windows.net/acrcssc-1.1.1rc5-py3-none-any.whl) for Continuous patching 

Run the following command:
```sh
az extension add --source <path to the wheel file>
```

## Enable the Continuous Patching Workflow
To enable Continuous Patching, follow the series of steps below that outline the CLI process. These guidelines detail the lifecycle of a continuous patching workflow, encompassing its creation to subsequent updates to eventual deletion. 
1. Login to Azure CLI with az login
```sh
az login
```
2. Login to ACR 
```sh
az acr login -n <myRegistry>
```
3.	Create a JSON configuration file with the following schema:
```sh
vim continuouspatching.json //any file creation command and file name will do
```
Here is the JSON Schema:
```sh
{
    "version": "v1",
    "tag-convention" : "<incremental|floating>",
    "repositories": [{
        "repository": "<Repository Name>",
        "tags": ["<comma-separated-tags>"],   
        "enabled": <true|false>
    }] 
}
```
The schema ingests specific repositories and tags in an array format. Each variable is defined below:

- "version" allows the ACR team to track what schema version you’re on. Do not change this variable unless instructed to.
- "tag-convention" this is an optional field. Allowed values are "incremental" or "floating" (defaults to incremental if not specified).
  - incremental = appends a structured -nnn suffix to the original tag, where -nnn increments from 1 to 999. The highest existing -nnn suffix will always be the one incremented for subsequent patches
  - floating = creates a single mutable tag formatted as "-patched" that is appended to the original tag and always points to the latest patched image

    We recommend using incremental tags as a more secure method of updating patches for deployments. Incremental tags are immutable, which aligns with cloud native best practices (easy rollbacks).

    As for floating tags, they can be used conveniently for builds at build time, since the consistent tag always points to the latest patch and allows teams to avoid the constant manual switch to new patched tags.
  
    Here's an example:
  
    ![PatchingTimelineExample](./media/patching_timeline_example1.png)

- "repositories" is an array that consists of all objects that detail the specific repository and tags
    - "repository" refers to repository name
    - "tags" is an array of tags separated by commas. The wildcard "*" can be used to signify all tags within that repository
    - "enabled" is a Boolean value of true or false determining if the specified repo is on or off

The following details an example configuration for a customer who wants to patch all tags(use the * symbol) within the repository “python”, and to patch specifically the “jammy-20240111” and “jammy-20240125” tags in the repository “ubuntu”. 

JSON example:
```json
{
"version": "v1",
"tag-convention" : "incremental",
"repositories": [{
        "repository": "python",
        "tags": ["*"],
        "enabled": true
    },
    {
        "repository": "ubuntu",
        "tags": ["jammy-20240111", "jammy-20240125"],
        "enabled": true, 
    }]
}
```
4. After creating your configuration file, it is recommended to execute a dry run to verify the intended artifacts are selected by the JSON criteria. The dry run requires a parameter called schedule, which specifies how often your continuous patching cycle will run. The schedule flag is measured in days, with a minimum value of 1 day, and a maximum value of 30 days. 

Command Schema:
```sh
az acr supply-chain workflow create -r <registryname> -g <resourcegroupname> -t continuouspatchv1 --config <JSONfilepath> --schedule <number of days> --dry-run 
```
Example Command 
```sh
az acr supply-chain workflow create -r myRegistry -g myResourceGroup -t continuouspatchv1 -–config ./continuouspatching.json --schedule 1d –-dry-run   
```
Help command to see all required/optional flags
```sh
az acr supply-chain workflow create --help
```
 
This command will output all specified artifacts by the JSON file configuration. Customers can verify that the right artifacts are selected. With the sample ubuntu configuration above, the following results should be displayed as output. 
```sh
Ubuntu: jammy-20240111
Ubuntu: jammy-20249125
```
5. Once satisfied with the dry-run results, run the ‘create’ command with a specified schedule to officially create your continuous patching workflow . Once run, the workflow will execute immediately on the specified artifacts. The workflow would then repeat per the cadence at the current time of execution. For example, if you run this command at 3:00 PM on January 26th with a cadence of 1d, the continuous patching workflow would run immediately on 3:00pm on January 26th, then again on January 27th at 3:00 PM, and again on January 28th at 3:00 PM. 

Command Schema
```sh
az acr supply-chain workflow create -r <registryname> -g <resourcegroupname> -t continuouspatchv1 -–config <JSONfilename> --schedule <number of days> 
```

Example Command 
```sh
az acr supply-chain workflow create -r myRegistry -g myResourceGroup -t continuouspatch v1 -–config ./continuouspatching.json --schedule 1d 
```

Help command to see all required/optional flags
```sh
az acr supply-chain workflow create --help
```

You should see a success message confirming that your workflow tasks have been queued. 

## Use Azure Portal to view workflow tasks

Once the workflow succeeds, go to the Azure Portal to view your running tasks. Click into Services -> Repositories, and you should see a new repository named “csscpolicies/patchpolicy”. This repository hosts the JSON configuration artifact that will be continuously referenced for continuous patching.  

![PortalRepos](./media/portal_repos1.png)

Next, click on “Tasks” under “Services”. You should see 3 new tasks, named the following:

![PortalTasks](./media/portal_tasks1.png)

- cssc-trigger-workflow – this task scans the configuration file and calls the scan task on each respective image.    
- cssc-scan-image – this task scans the image and calls the patching task after
- cssc-patch-image – this task patches the image
These tasks work in conjunction to execute your continuous patching workflow.

You can also click on “Runs” within the “Tasks” view to see specific task runs. Here you can view status information on whether the task succeeded or failed, along with viewing a debug log. 

![PortalRun](./media/portal_runs1.png)

## Use CLI to view workflow tasks

You can also run the following CLI show command to see more details on each task and the general workflow. The command will output
- Schedule
- Creation date
- System data such as last modified date, by who, etc.

Command Schema
```sh
az acr supply-chain workflow show -r <registry> -g <resourceGroup> -t continuouspatchv1   
```
Example Command 
```sh
az acr supply-chain workflow show -r myRegistry -g myResourceGroup -t continuouspatchv1 
```
Help command to see all required/optional flags
```sh
az acr supply-chain workflow show --help
```

## Updating the Continuous Patching Workflow

In scenarios where you want to make edits to your continuous patching workflow, the update command is the easiest way to do so. You can update your schedule or JSON config schema with the update CLI command directly. 

Command Schema
```sh
az acr supply-chain workflow update -r <registry> -g <resourceGroup> -t continuouspatchv1 --config <JSONfilename> --schedule <number of days>
```
Example Command 
```sh
az acr supply-chain workflow update -r myRegistry -g myResourceGroup -t continuouspatchv1 --config ./continuouspatching.json --schedule 1d
```
Help command to see all required/optional flags
```sh
az acr supply-chain workflow update --help
```

To update your schedule, run the previous command with a new input for schedule. To update your JSON configuration, we recommend making changes to the file, running a dry-run, and running the update command. 

You can verify the updated workflow configuration by running the following show command or by clicking into your registry portal view. 
```sh
az acr supply-chain workflow show -r myregistry -g myresourcegroup -t continuouspatchv1
```

**NOTE:**
Run Immediately: When Create and Update commands are executed, new Tasks will be queued based on the specified "schedule". This is behavior is intentional to avoid unnecessary patch runs for small workflow configuration updates that could incur expensive costs. If an immediate run is desired, the [--run-immediately] parameter may be provided.

## Deleting the Continuous Patching Workflow

To delete the continuous patching workflow, please run the following CLI command

Command Schema
```sh
az acr supply-chain workflow delete -r <registry> -g <resourceGroup> -t continuouspatchv1 
```
Example Command 
```sh
az acr supply-chain workflow delete -r myregistry -g myresourcegroup –t continuouspatchv1
```
Help command to see all required/optional flags
```sh
az acr supply-chain workflow delete --help
```

Once a workflow is successfully deleted, the repository “csscpolicies/patchpolicy” will be automatically deleted. The 3 tasks that run your workflow will also be automatically deleted, along with any currently queued runs. 

## Listing Running Tasks

To list the most recently executed CSSC tasks, the following List command is available:
```sh
az acr supply-chain workflow list -r <registryname> -g <resourcegroup> [–-run-status <failed || successful || running>]
```

A successful result will return the following information:
-	Image name and tag
-	Workflow type
-	Scan status
-	Last scan date and time (if status failed, date would be left blank)
-	Scan task ID (for further debugging)
-	Patch Status
-	Last patch date and time (if status failed, date would be left blank)
-	Patched image name + tag
-	Patch task ID (for further debugging)

Example
```sh
ubuntu:jammy-20240111
scan status: successful
scan date: 2024-07-02T14:02:00
scan task ID: abc
patch status: successful
patch date: 2024-07-02T14:04:00
patch task id: def
patched image: ubuntu:jammy-20240111-1
workflow type: continuouspatchv1
```
The [--run-status] will return all tasks statuses that match the specified filter. This CLI command provides important debugging information.
For example, If the "failed" value is specified under run-status, only images which have failed their patching will be listed.
In the examples below, you’ll see a “skipped” patch status in certain scenarios. This is used to describe when a scan occurred, but no patch was required because no OS vulnerabilities were found (“Skipped” statuses are considered as successful).

**Possible CLI Output Scenarios**

If scan and patch are successful
```sh
image: import:dotnetapp-manual
        scan status: Succeeded
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: Succeeded
        patch date: 2024-09-13 21:07:32.841962+00:00
        patch task ID: xyz2
        last patched image: import:dotnetapp-manual-patched
        workflow type: continuouspatchv1
```

If scan is successful but patch isn’t (with a previous patched image available)
```sh
image: import:dotnetapp-manual
        scan status: Succeeded
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: Failed
        patch date: 2024-09-13 21:07:32.841962+00:00
        patch task ID: xyz2
        last patched image: import:dotnetapp-manual-patched
        workflow type: continuouspatchv1
```

If scan is successful but patch isn’t (with NO previous patched image available)
```sh
image: import:dotnetapp-manual
        scan status: Succeeded
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: Failed
        patch date: 2024-09-13 21:07:32.841962+00:00
        patch task ID: xyz2
        last patched image: ---No patch image available---
        workflow type: continuouspatchv1
```

If scan is successful and no patch is needed (no OS vulnerabilities found)
```sh
image: import:dotnetapp-manual
        scan status: Succeeded
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: Skipped
        skipped patch reason: no vulnerability found in the image import:dotnetapp-manual image: 
        patch date: ---Not Available---
        patch task ID: ---Not Available---
        last patched image: import:dotnetapp-manual-patched
        workflow type: continuouspatchv1
```

if scan is successful and no patch is needed and NO patched image exists yet
```sh
image: import:dotnetapp-manual
        scan status: Succeeded
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: Skipped
        skipped patch reason: no vulnerability found in the image import:dotnetapp-manual image: 
        patch date: ---Not Available---
        patch task ID: ---Not Available---
        last patched image: ---Not Available---
        workflow type: continuouspatchv1
```

If scan is a failure and a patched image exists
```sh
image: import:dotnetapp-manual
        scan status: Failed
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: ---Not Available---
        patch date: ---Not Available---
        patch task ID: ---Not Available---
        last patched image: import:dotnetapp-manual-patched
        workflow type: continuouspatchv1
```

If scan is a failure and NO previous patched image exists
```sh
image: import:dotnetapp-manual
        scan status: Failed
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: ---Not Available---
        patch date: ---Not Available---
        patch task ID: ---Not Available---
        last patched image: ---Not Available---
        workflow type: continuouspatchv1
```

If scan is currently running and a patched image exists
```sh
image: import:dotnetapp-manual
        scan status: Running
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: ---Not Available---
        patch date: ---Not Available---
        patch task ID: ---Not Available---
        last patched image: import:dotnetapp-manual-patched
        workflow type: continuouspatchv1
```

If scan is currently running and NO patched image exists
```sh
image: import:dotnetapp-manual
        scan status: Running
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: ---Not Available---
        patch date: ---Not Available---
        patch task ID: ---Not Available---
        last patched image: ---Not Available---
        workflow type: continuouspatchv1
```

If patch is currently running and a patched image exists
```sh
image: import:dotnetapp-manual
        scan status: Succeeded
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: Running
        patch date: 2024-09-13 21:07:32.841962+00:00
        patch task ID: xyz2
        last patched image: import:dotnetapp-manual-patched
        workflow type: continuouspatchv1
```

If patch is currently running and NO patched image exists
```sh
image: import:dotnetapp-manual
        scan status: Succeeded
        scan date: 2024-09-13 21:05:58.841962+00:00
        scan task ID: dt21
        patch status: Running
        patch date: 2024-09-13 21:07:32.841962+00:00
        patch task ID: xyz2
        last patched image: ---Not Available---
        workflow type: continuouspatchv1
```

## Canceling Running Tasks

Sometimes you may need to cancel tasks which are currently running or waiting to run. For this purpose, please run the following CLI command:
```sh
az acr supply-chain workflow cancel-run -r <registryname> -g <resourcegroup> --type <continuouspatchv1>
```

This command will cancel all CSSC tasks within the registry with a status of “Running”, “Queued” and “Started”. The command will output a success or failure. Failure results will follow the failure pattern of the other workflow commands if the input is incorrect.
Running the cancel command will only affect tasks in the current schedule. For example, if a user has their schedule for 1d, and runs the cancel command, tasks in those 3 states will be canceled for that day, but will be requeued for the next day. If the schedule was a week, then that week’s tasks would be canceled, but the following week would have the tasks requeued. The common scenario for using this command is to cancel all running tasks in case a user misconfigures their continuous patching workflow and doesn’t want to wait for all tasks to finish running.

## Tags

- Tags suffixed with -1 to -999 are considered patch tags. Original tag and next patch is determined by splitting on the last occurance of '-'.
- Tags suffixed with -x where x > 999 are considered original tags and their patches will be created suffixed with -x between 1 to 999. For eg: jammy-20240530 will be considered as original because even though it ends with a number, 20240530 is greater than 999. Its first patch will be created with tag jammy-20240530-1 and so on.
- Logic is the same when tags = * or when tags are explicitly specified
- For every matching tag, latest patch tag is determined based on tag-convention
If tag-convention = incremental, only incremental patch tags (if any) are considered to determine the latest patch tag
If tag-convention = floating, only floating patch tag (if any) will be considered to determine the latest patch tag

Samples:

| Registry                | Repo         | Current Tags    | Expected Incremental Patch Tag          
| :--------------------- | :--------     | :---------------| :---------------
| myregistry.azurecr.io   | python       | 3.11<br>3.11-slim-bookworm   | 3.11-1<br>3.11-slim-bookworm-1<br>       
| myregistry.azurecr.io   | myrep/ubuntu | jammy<br>jammy-1<br>jammy-2<br>jammy-patched<br>jammy-20240530<br>jammy-20240530-1<br>jammy-20240530-2<br>jammy-20240530-patched|  jammy-3<br>jammy-20240530-3<br>

## Troubleshooting Tips

Use the task list command to output all failed tasks. Specifying the “cssc-patch” command is best for failure. The documentation on the task-list [command](https://learn.microsoft.com/en-us/cli/azure/acr/task?view=azure-cli-latest#az-acr-task-list-runs) is here. 

Task-list command for top 10 failed patch tasks
```sh
az acr task list-runs -r registryname -n cssc-patch-image --run-status Failed --top 10
```

This command will output all failed tasks. To investigate a specific failure, grab the runID that’s outputted from this command and run
```sh
az acr task logs -r registryname --run-id <run-id>
```
If the logs aren’t sufficient, or an issue is persistent, or for any feedback, please reach out to the ACR team on this [Teams channel](https://teams.microsoft.com/l/channel/19%3A48363910c4d148548fb118611083aaa6%40thread.tacv2/Feedback?groupId=29f3f933-a473-4ab2-807b-69185481de10&tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47). 


## FAQs

- Getting error saying "An error occurred. Pip failed with status code 1. Use --debug for more information". while trying to install the extension ?

  **Probable cause**:
  Wheel File must not be renamed. If you download the file in the same location, default behavior is it will duplicate file with same file name and append " (n)" 
  (where n is a number) to it, e.g. "**acrcssc-1.0.0b1-py3-none-any.whl**" can get renamed to **acrcssc-1.0.0b1-py3-none-any (1).whl**. So, ensure the filename 
  remains intact without ***" (n)"***.

- Which types of OS are supported?

  Only linux based images are supported as of now.

- Can I patch images that are EOSL (End Of Service Life) ?

  EOSL means that the software provider is no longer offerring updates, security patches or technical support. E.g. Debian 8, Fedora 28, etc. Images for which OS 
  has reached EOSL, those would be skipped from the scan. Note: It doesn't mean they are vulnerable free. So, you should look to upgrade to a higher version.
