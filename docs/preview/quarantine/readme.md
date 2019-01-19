# Quarantine Pattern
To assure a registry only contains images that have been vulnerability scanned, ACR introduces the Quarantine pattern.
When a registries policy is set to Quarantine Enabled, all images pushed to that registry are put in quarantine by default. Only after the image has been verifed, and the quarantine flag removed may a subsequent pull be completed.
 
> Note: This is an early preview of this workflow. Additional capabilities will be added, including CLI and Portal support.
 
# Current Workflow

## Quarantined Webhook Notification

No matter if you have quarantine flow enabled or not, scanner can always subscribe to the "quarantine" webhook. when an image is pushed, we will try to notify the matching "quarantine" webhook, with payload as below:

```json
{
  "id": "0d799b14-404b-4859-b2f6-50c5ee2a2c3a",
  "timestamp": "2018-02-28T00:42:54.4509516Z",
  "action": "quarantine",
  "target": {
    "size": 1791,
    "digest": "sha256:91ef6",
    "length": 1791,
    "repository": "helloworld",
    "tag": "1"},
  "request": {
    "id": "978fc988-zzz-yyyy-xxxx-4f6e331d1591",
    "host": "[registry].azurecr.io",
    "method": "PUT"}
}
```

You can use our [management Webhook API](https://docs.microsoft.com/en-us/rest/api/containerregistry/webhooks/create) to create and subscribe for the "quarantine" webhook (the actions field need to use the "quarantine" action).

>>In order to call the above management API, you need to get an access token which is be used as the Authorization header . Here is an [example](https://blogs.technet.microsoft.com/stefan_stranger/2016/10/21/using-the-azure-arm-rest-apin-get-access-token/) on how to get access token for a Service Principal:

>>
```
curl --request POST "https://login.windows.net/{TenantId}/oauth2/token" --data-urlencode "resource=https://management.core.windows.net" --data-urlencode "client_id={appId}" --data-urlencode "grant_type=client_credentials" --data-urlencode "client_secret={app secret}"
```

Before Quarantine is configured on the registry, both "quarantine" and "push" webhook will be raised for each image push. The scanner can subscribe to "quarantine" webhook and conduct security scan for the newly pushed image; while normal user can subscribe to the normal "push" webhook and pull the image successfully.

## Configure Quarantine on a registry

Once the user decides to enable Quarantine on a registry, he can use our [management Policy API](https://docs.microsoft.com/en-us/rest/api/containerregistry/registries/updatepolicies).

Once Quarantine is enabled on a registry, for newly pushed image, it will enter quarantine state automatically and only a special user can see it. Meanwhile, the same "quarantine" webhook will be raised, but no "push" notification anymore. This gives the scanner a chance to scan the image first before making it available to other users. 

Once scanner finishes scanning the image, it can mark the image as good, which will make this image available to all other users. Meanwhile a "push" notification is generated so that other users are notified.

>Please note, once the Quarantine is enabled, any images without being marked as good will be blocked for pull. This may impact user's ongoing workflow. We would recommend that before enable Qurantine mode on the registry, the scanner should finish scanning all the existing images (this can be done by using catalog API and manifest list API). User can then look at the failed images and decide if he should enable the Quarantine mode.

The detailed flow is described below.
 
## name your registry
```bash
export ACR_NAME=quarantine
export REGISTRY_NAME=$ACR_NAME.azurecr.io
```

## Login to the dogfood registry
`docker login $REGISTRY_NAME`

## Push an image

```
docker push  ${REGISTRY_NAME}helloworld:1
 96c922e98de8: Pushed
 digest: sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d size: 524
```

The image is now quarantined

## Attempt to pull the quarantined image

```
docker pull  ${REGISTRY_NAME}helloworld:1
Error response from daemon: manifest for quarantinetest1.azurecr.io/helloworld:1 not found
```

## Attempt to pull the image by its digest

```
docker pull ${REGISTRY_NAME}helloworld@sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
		Error response from daemon: unknown: The operation is disallowed.
```

## Quarantined Webhook Notification

Once the image is pushed, you will receive a notification through webhooks.

## Pull the quarantined image

Once the image is quarantined, you will need user with the **AcrQuarantineReader** role. The presumption here is the Vulnerability Scanning solution is configured to use this account.

`docker login ${REGISTRY_NAME} -u` **[quarantinedServicePrincpalUsr]**` -p `**[quarantinedServicePrincpalPwd]**

Now the user can pull the quarantined image by digest

```
docker pull [registry].azurecr.io/helloworld@sha256:sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
  Pulling from helloworld
	Digest: sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
	Status: Image is up to date for [registry].azurecr.io/helloworld@sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
```

## Query Attributes of an Image (manifest)

Querying ACR metadata via the REST API requires an OAuth Token.

To query attributes via REST, use the following workflow:

1. encode the username/password with the required permissions
1. get an access token
1. query acr metadata for the attributes of a given digest

### Encode the username and password 
  - using a tool like https://www.base64encode.org/
    - Encode using the following format: **[username]**:**[password]**
    - Copy the encoded value

### Get an  access token for the user

- Using a REST client, like Postman, query for the OAuth token:

    REST format: `https://`**[login-url]**`/oauth2/token?service=`**[login-url]**`&scope=repository:`**[image]**`:pull`

- Set the header for Authorization, setting the 'Basic' word followed by a space and the encoded usr:pwd value

    |Header | Value |
    |-------|-------|
    | Authorization | Basic [base64 encoded usr:pwd] |
    | Host | [login-url] |

    example: 
    ```
    GET https://quarantinetest1.azurecr.io/oauth2/token?service=quarantinetest1.azurecr.io&scope=repository:helloworld:pull
    ```


### Query the metadata API

- With an OAuth token, we can now query ACR metadata:

    REST format: `https://`**[login-url]**`/acr/v1/`**[image]**`/_manifests/`**[digest]**

- Set the header for Authorization, setting the OAuth token

    |Header | Value |
    |-------|-------|
    | Authorization | Bearer [token] |
    | Host | [login-url] |

    example: 
    ```
    GET https://quarantinetest1.azurecr.io/acr/v1/mytest/_manifests/sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
    ```
## Remove the Quarantine Flag

Once a scan completes, a user with the **AcrQuarantineWriter** role can update the manifest attribute to removed the quarantined flag.

### Encode the username and password 
  - using a tool like https://www.base64encode.org/
    - Encode using the following format: **[username]**:**[password]**
    - Copy the encoded value

### Get a Push access token for the user
Using a REST client, like Postman, query for the OAuth token:

**REST format:** `https://`**[login-url]**`/oauth2/token?service=`**[login-url]**`&scope=repository:`**[image]**`:pull,push`

Set the header for Authorization, setting the 'Basic' word followed by a space and the encoded usr:pwd value

    |Header | Value |
    |-------|-------|
    | Authorization | Basic [base64 encoded usr:pwd] |
    | Host | [login-url] |

    example: 
    ```
    GET https://quarantinetest1.azurecr.io/oauth2/token?service=quarantinetest1.azurecr-test.io&scope=repository:helloworld:pull,push
    ```

### Remove the Quarantine Flag

- Update manifest attributes using the access token.    
    REST format: `PATCH https://`**[login-url]**`acr/v1`**[image]**`/_manifests/`**[digest]**

    Payload:
    ```json
        {
            "quarantineState": "[Passed|Failed]",
            "quarantineDetails": "[json string of detailed results]"}
        }
    ```
	> Note: the quarantineDetails schema is defined at [here](https://github.com/sajayantony/image-scanner-specs/blob/master/summary/schema.json)

    |Header | Value |
    |-------|-------|
    | Authorization | Bearer [token] |
    | Host | [login-url] |

    example: 

	PATCH https://quarantinetest1.azurecr.io/acr/v1/mytest/_manifests/sha256:80f0d5c8786bb9e621a45ece0db56d11cdc624ad20da9fe62e9d25490f331d7d HTTP/1.1
    ```json
        {
            "quarantineState": "Passed", 
            "quarantineDetails": "{\"state\":\"scan passed\",\"link\":\"http://test.io/test\"}"
        }
    ```

## Image Pushed Webhook Notification

Based on the registry policy, once the image has been set to **Passed**, a Image Pushed webhook will triggered

## Image Pull

With the quarantine removed, a user with standard **reader** role can pull the image, using the tag

```
docker login ${REGISTRY_NAME}
docker pull ${REGISTRY_NAME}helloworld:1
```
