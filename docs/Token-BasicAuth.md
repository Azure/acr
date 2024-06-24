---
type: post
title: "Token with Basic Auth"
---

# Azure Container Registry's support of getting Bearer token using Basic Authentication

The Azure Container Registry supports both Basic Authentication and OAuth2 for getting a registry Bearer token. This document describes how to get a Bearer token using Basic Authentication. To get the token using OAuth2, please refer to the [AAD-OAuth doc](https://github.com/Azure/acr/blob/master/docs/AAD-OAuth.md).

## Using the token API

ACR has implemented the GET method on the token endpoint for user to retrieve a Bearer token using Basic Authentication:

GET /oauth2/token


### Get the scope of the token to be requested

The first thing you want is to obtain an authentication challenge for the operation you want to on the Azure Container Registry. That can be done by targetting the API you want to call without any authentication. Here's how to do that via `curl`:

```bash
export registry="contosoregistry.azurecr.io"
curl -v https://$registry/v2/hello-world/manifests/latest
```

Note that `curl` by default does the request as a `GET` unless you specify a different verb with the `-X` modifier.

This will output the following payload, with `...` used to shorten it for illustrative purposes:

```bash
< HTTP/1.1 401 Unauthorized
...
< Www-Authenticate: Bearer realm="https://contosoregistry.azurecr.io/oauth2/token",service="contosoregistry.azurecr.io",scope="repository:hello-world:pull"
...
{"errors":[{"code":"UNAUTHORIZED","message":"authentication required","detail":[{"Type":"repository","Name":"hello-world","Action":"pull"}]}]}
```

Notice the response payload has a header called `Www-Authenticate` that gives us the following information:
  - The type of challenge: `Bearer`.
  - The realm of the challenge: `https://contosoregistry.azurecr.io/oauth2/token`.
  - The service of the challenge: `contosoregistry.azurecr.io`.
  - The scope of the challenge: `repository:hello-world:pull`.

The body of the payload might provide additional details, but all the information you need is contained in the `Www-Authenticate` header.

With this information we're now ready to call `GET /oauth2/token` to obtain an ACR access token that will allow us to use the `GET /v2/hello-world/manifests/latest` API. 

### Encode the username and password 
  - You can use Windows Powershell or `base64` command line utility in Linux/Mac
    - Encode using the following format: **[username]**:**[password]**
    - Powershell: 
    	- `[convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('[username]:[password]'))`
    - Linux/Mac Terminal: 
    	- `echo -n '[username]:[password]' | base64`
    - Copy the encoded value and set it as a environment variable
	
```bash
export acr_credential="xxxxxxx"
```

### Get a Pull access token for the user

**REST format:** `https://`**[login-url]**`/oauth2/token?service=`**[login-url]**`&scope=repository:`**[image]**`:pull,push`

Set the header for Authorization, setting the 'Basic' word followed by a space and the encoded usr:pwd value

    |Header | Value |
    |-------|-------|
    | Authorization | Basic [base64 encoded usr:pwd] |
    | Host | [login-url] |

Here's how such a call looks when done via `curl`:

```bash
export registry="contosoregistry.azurecr.io"
export scope="repository:hello-world:pull"
curl -v -H "Authorization: Basic $acr_credential" \
"https://$registry/oauth2/token?service=$registry&scope=$scope"
```

The outcome of this operation will be a response with status 200 OK and a body with the following JSON payload:
```json
{"access_token":"eyJ...xcg"}
```

This response is the ACR access token which you can inspect with [jwt.ms](https://jwt.ms/). You can now use it to call APIs exposed by the Azure Container Registry.

### Calling an Azure Container Registry API

In this example we'll call the `GET /v2/{repository}/manifests/{tag}` API on an Azure Container Registry. Assume you have the following:
  1. A valid container registry, which here we'll call `contosoregistry.azurecr.io`.
  2. A valid ACR access token, created with the correct scope for the API we're going to call.

Here's how a call to the `GET /v2/hello-world/manifests/latest` API of the given registry would look like when done via `curl`:

```bash
export registry="contosoregistry.azurecr.io"
export acr_access_token="eyJ...xcg"
curl -v -H "Authorization: Bearer $acr_access_token" -H "Accept:application/vnd.oci.image.manifest.v1+json" https://$registry/v2/hello-world/manifests/latest
```
This should result in a status 200 OK.
