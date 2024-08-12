---
title: Introducing Azure Container Registry Repository Permissions through Attribute-Based Access Control (Private Preview)
description: Learn about the new Repository Permissions feature for Azure Container Registry during the private preview. The feature ensures secure and efficient repository permissions management for Azure Container Registry.
ms.topic: whats-new #Don't change.
ms.date: 08/12/2024
ms.author: johsh
author: johnsonshi
ms.service: container-registry
---

# What's New: Manage Repository Permissions for Azure Container Registry through Attribute-Based Access Control (ABAC)

[!NOTE] The Repository Permissions feature for Azure Container Registry is currently in private preview. For details on enrolling in the Private Preview and to ensure a smooth experience, please follow the provided instructions.

If you're looking to stay updated with the latest enhancements in Azure Container Registry (ACR), particularly in managing repository permissions, this article is for you. We are excited to announce the private preview of managing repository permissions in ACR in Azure role assignments, a feature that transforms how you manage access to your repositories.

Azure Attribute-Based Access Control (ABAC) allows for more granular repository-level permissions during Azure role assignments with Entra identities. During Azure Entra role assignments, role permissions can be scoped to specific repositories within a registry rather granting permissions to the entire registry. This feature improves the security footprint by ensuring permissions are precisely assigned according to your needs.

Understanding the new ACR ABAC Repository Permissions will help you optimize your workflow and enhance your security measures. So, let's dive in and explore what's new!

## Azure Attribute-Based Access Control (ABAC) capabilities

Azure Attribute-Based Access Control (ABAC) builds on top of Azure RBAC by allowing repository conditions during Azure Entra role assignments for ACR.

- **Condition-based Role Assignments**: Azure ABAC lets you [specify repository conditions for Azure Entra role assignments](https://learn.microsoft.com/en-us/azure/role-based-access-control/conditions-overview), scoping role permissions to specific repositories based on repository name conditions.
- **Repository Name Conditions**: You can grant access to repositories matching certain prefixes or exact names, tailoring permissions to your organizational needs.
- **Compatibility with Roles**: ABAC conditions work with both [built-in ACR roles](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-roles) and custom role assignments, providing flexibility in repository permission management during Azure Entra role assignments.
- **Identity Support**: ABAC Repository Permissions support various Microsoft Entra ID identities, including users, groups, service principals, and managed identities, ensuring comprehensive access control for all role assignment scenarios.
- **SKU Support**: All ACR SKUs support ABAC, making it available across different service levels.

This feature is a significant step towards more secure and precise access management within Azure Container Registry.

## Related content

For private preview onboarding and documentation, please visit [Access-Based Access Control for Azure Container Registry Repository Permissions (Private Preview)](../preview/abac-repo-permissions/README.md).
