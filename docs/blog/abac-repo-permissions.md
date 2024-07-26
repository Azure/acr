---
title: ACR ABAC Repository Permissions Private Preview Blog Announcement
description: Private preview blog announcement for the ACR ABAC Repository Permissions feature.
ms.topic: post
ms.date: 07/15/2024
ms.author: johsh
author: johnsonshi
ms.custom:
---

> [!NOTE]
> This feature is available as a private preview.

## Announcing Private Preview - ACR ABAC Repository Permissions

We are excited to bring you the private preview of the Azure Container Registry (ACR) ABAC Repository Permissions!

Azure attribute-based access control (Azure ABAC) enables you to scope permissions to repositories within a registry when assigning roles. It improves your security footprint by granting permissions to specific repositories instead of the entire registry.

Azure ABAC builds on Azure role-based access control (Azure RBAC) by allowing you to [specify conditions when authoring Azure role assignments](https://learn.microsoft.com/en-us/azure/role-based-access-control/conditions-overview). ABAC conditions can scope role assignment permissions to specific repositories (in a registry) based on repository name conditions. For instance, you can choose to grant role assignment access only to repository names that match a specific prefix or have an exact name match.

ABAC conditions can be used with both [ACR built-in](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-roles?tabs=azure-cli) and custom role assignments. It can also be used with all forms of Microsoft Entra ID identities such as users, groups, service principals, and managed identities. All ACR SKUs support this capability.

For private preview onboarding and documentation, please visit [ABAC Repository Permissions](../preview/abac-repo-permissions/README.md).

---
Johnson Shi
