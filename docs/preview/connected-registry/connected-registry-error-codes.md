---
title: Connected registry error code reference
description: Details about error codes shown in the statusDetails property of a connected registry resource. For each error, possible solutions are listed.
ms.topic: article
ms.date: 04/13/2021
ms.author: jeburke
author: jaysterp
---

# Connected registry error code reference

This article helps you troubleshoot error codes you might encounter in the `StatusDetails` property of a connected registry.

## Status Details Format

When a connected registry has a connection state of `Unhealthy`, this indicates there is a critical error on the instance running on-premises. You may reference the `StatusDetails` property of the connected registry resource to view the corresponding error.

Run the `az acr connected-registry show` command to view the statusDetails property for your connected registry.

`StatusDetails` provides a list of errors, each with the following format:

```json
{
  "code": "Error code",
  "correlationId": "CorrelationId of the error on the on-premises connected registry instance",
  "description": "Description corresponding to this error",
  "timestamp": "Timestamp corresponding to this error",
  "type": "Component of the connected registry instance corresponding to the error"
}
```

Every time the connected registry instance syncs with the cloud, these status details are updated. When the connected registry no longer has status details listed, it is considered healthy and its connection state is transitioned from `Unhealthy` to `Online`.

## Error Code Reference

This section lists the possible codes you may see in the `StatusDetails` property of a connected registry, which indicate critical errors. For each error, possible solutions are listed.
