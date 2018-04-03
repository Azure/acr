# Quarantine Pattern
To assure a registry only contains images that have been vulnerability scanned, ACR introduces the Quarantine pattern.
When a registries policy is set to Quarantine Enabled, all images pushed to that registry are put in quarantine by default. Only after the image has been verifed, and the quarantine flag removed may a subsequent pull be completed.
 
> Note: This is an early preview of this workflow. Additional capabilities will be added, including CLI support. As of this point (March 26, 2018), a REST API is supported to bring images in/out of quarantine. Additionally, new webhooks will be enabled to notify vulnerability scanning solutions an image is available.
 
# Current Workflow

## Configure Quarantine on a registry

> The quarantine flag is not yet exposed. Contact SteveLas@Microsoft.com to have your subscription/registry enabled. 

## Login to the dogfood registry
`docker login [registry].azurecr-test.io`

> For this early preview, use the username/password and registry provided.

## Push an image

```
docker push  [registry].azurecr-test.io/helloworld:1
 96c922e98de8: Pushed
 digest: sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d size: 524
```

The image is now quarantined

## Attempt to pul the quarantined image

```
docker pull  [registry].azurecr-test.io/helloworld:1
Error response from daemon: manifest for quarantinetest1.azurecr-test.io/helloworld:1 not found
```

## Attempt to pull the image by its digest

```
docker pull [registry].azurecr-test.io/helloworld@sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
		Error response from daemon: unknown: The operation is disallowed.
```

## Quarantined Webhook Notification

> Note: webhook is not yet enabled

Once the image is pushed and in quarantine state, you will receive a notification through webhooks, as below:
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
    "host": "[registry]azurecr-test.azurecr.io",
    "method": "PUT"}
}
```

## Pull the quarantined image

Once the image is quarantined, you will need user with the **AcrQuarantineReader** role. The presumption here is the Vulnerability Scanning solution is configured to use this account.

`docker login [registry].azurecr-test.io -u` **[quarantinedServicePrincpalUsr]**` -p `**[quarantinedServicePrincpalPwd]**

Now the user can pull the quarantined image by digest

```
docker pull [registry].azurecr-test.io/helloworld@sha256:sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
  Pulling from helloworld
	Digest: sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
	Status: Image is up to date for [registry].azurecr-test.io/helloworld@sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
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
    GET https://quarantinetest1.azurecr-test.io/oauth2/token?service=quarantinetest1.azurecr-test.io&scope=repository:helloworld:pull
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
    GET https://quarantinetest1.azurecr-test.io/acr/v1/mytest/_manifests/sha256:80f0d5cxxxxXxxXxxxxece0db56d11cdc624ad20da9fe62d7d
    ```
## Remove the Quarantine Flag

Once a scan completes, a user with the **AcrQuarantineWriter** role can update the manifest attribute to removed the quarantined flag.

### Encode the username and password 
  - using a tool like https://www.base64encode.org/
    - Encode using the following format: **[username]**:**[password]**
    - Copy the encoded value

### Get an  access token for the user

- Using a REST client, like Postman, query for the OAuth token:

    REST format: `https://`**[login-url]**`/oauth2/token?service=`**[login-url]**`&scope=repository:`**[image]**`:pull,push`

- Set the header for Authorization, setting the 'Basic' word followed by a space and the encoded usr:pwd value

    |Header | Value |
    |-------|-------|
    | Authorization | Basic [base64 encoded usr:pwd] |
    | Host | [login-url] |

    example: 
    ```
    GET https://quarantinetest1.azurecr-test.io/oauth2/token?service=quarantinetest1.azurecr-test.io&scope=repository:helloworld:pull,push
    ```

### Remove the Quarantine Flag

- Update manifest attributes using the access token.    
    REST format: `PATCH https://`**[login-url]**`acr/v1`**[image]**`/_manifests/`**[digest]**
    Payload:
    ```json
        {
            "quarantineState": "[ScanStarted|ScanSucceeded|ScanFailed]", 
            "quarantineDetails": "[url for a full quarantine report]"}
        }
    ```
    |Header | Value |
    |-------|-------|
    | Authorization | Bearer [token] |
    | Host | [login-url] |

    example: 

	PATCH https://quarantinetest1.azurecr-test.io/acr/v1/mytest/_manifests/sha256:80f0d5c8786bb9e621a45ece0db56d11cdc624ad20da9fe62e9d25490f331d7d HTTP/1.1
    ```json
        {
            "quarantineState": "ScanSucceeded", 
            "quarantineDetails": "http://test.io/test"}
        }
    ```

    > Note: we will expand this JSON to support versioned MIME types, such as Grafeas". For now, **quarantinereport** is a simple text field.

## Image Pushed Webhook Notification

Based on the registry policy, once the image has been set to **ScanSucceeded**, a Image Pushed webhook will triggered 

## Image Pull

With the quarantine removed, a user with standard **reader** role can pull the image, using the tag

```
docker login [registry].azurecr-test.io
docker pull [registry].azurecr-test.io/helloworld:1
```

# More Info
For more info on the ACR metadata API, see [metadata api](../metadata/metadataapi.md)
