# How to use a custom regisry ID

Azure docker registries has a typical ID of the format `*-microsoft.azurecr.io`. A customer might like to have a custom registry ID that associate with its own organization. The following is the guide on how to achieve that.

## Prerequisites

To setup custom registry ID, the followings are needed.

* (Note that this prerequisite and all steps related to this prerequisite will be subject to change because we will include steps in how to set this up in V1.01 of this guide) A front end docker host whichs hostname is desired regisry ID. We will run an nginx docker image on it. In this guide we would use the example: `registry.azurecr-test.io`
* SSL certificate for `registry.azurecr-test.io` with filename `cert.crt`. Private key file, with filename `private.key`. The pass-phrase of the certificate stored in a text file, with filename `pwd`
* An instance of Azure Docker Registry service as the backend. In this example we would use `peterhsuacr-microsoft.azurecr.io`

## Steps

1. On `registry.azurecr-test.io`, copy [setup files](custom-id/) into it.
2. Create a sub-directory named `ssl`, copy `cert.crt`, `private.key`, and `pwd` into this directory. The location of these files can be changed but the [docker-compose.yml](custom-id/docker-compose.yml) would then need to be updated to reflect the changes.
3. Edit [custom_id.env](custom-id/custom_id.env) to point to your front end docker host (E.g. `registry.azurecr-test.io`) and backend Azure Docker Registry (E.g. `peterhsuacr-microsoft.azurecr.io`).
4. Run `docker-compose up -d` and this should bring up the nginx service that would forward all docker requests from your front end service to the backend Azure Docker Registry.

## Quick verification

A simple way to test the setup is to call `docker login` to quickly confirm that the requests are properly forwarded.

`docker login registry.azurecr-test.io -u myUserName -p 123456`
