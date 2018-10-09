## Getting Started with Container Registry - Manage Container Registry Task - in Java ##

* Create an Azure Container Registry.
* Schedule a new run to build a container image and push it to the registry.
* Wait for the run completion and download the run log.
* Create a task and queue a new run using the task.
* Schedule a new multi-step task run.
* List runs in the registry.

## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-java/blob/master/AUTH.md).

    mvn clean compile exec:java

## More information ##

[http://azure.com/java](http://azure.com/java)

If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212).