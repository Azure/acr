
---
services: Containerregistry
platforms: java
author: northtyphoon
---

## Getting Started with Containerregistry - Manage Container Registry Build - in Java ##

* Create an Azure Container Registry.
* Queue a new build to build a container image and push it to the registry.
* Wait for the build completion and download the build log.

## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-java/blob/master/AUTH.md).

    mvn clean compile exec:java

## More information ##

[http://azure.com/java](http://azure.com/java)

If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)