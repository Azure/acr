# ACR Helm Repo Security Advisory

|Date | Status |
|-|-|
| September 22, 2020| Active - testing solution |
| September 18, 2020 | Identified |

- The ACR engineering team is validating a fix that has been deployed for canary testing on September 22, 2020
- Assuming testing goes as expected, following Azure safe deployment practices, regional deployments will start September 23, 2020
- Deployments should complete by start of business, Monday September 28, 2020 - **_please check here for final confirmation_**
- Azure government clouds and Azure China should complete by September 30, 2020.

## Does this Issue Apply to You

- Using `helm repo` features with Azure Container Registry - yes
- Using the helm client, version `v3.3.1` ***or lower*** - yes
- Using **[helm 3 registry](https://helm.sh/docs/topics/registries/)** features that persist helm charts as OCI Artifacts - **this does NOT apply** to you

## Issue Summary

- ACR generates yaml content within the `chart.yaml` index to identify where the chart is stored within the content store
  ```yaml
  apiVersion: v1
  entries:
  wordpress:
  - acrMetadata:
      manifestDigest: sha256:08ef434162070bba4256414c80b001d15b7503ef2a1a4fa1f60bab174f80d4d7
    appVersion: 5.1.0
    created: "2019-03-06T16:59:25.8892193Z"
  ```
- To mitigate security concerns, unrelated to how ACR annotates `chart.yaml`, the helm client no longer supports additional content within the `chart.yaml` index file causing helm v3.3.2 or higher to fail.
- ACR is actively rolling out a server side change to generate newly complaint content, expected to be completed by Monday September 28, 2020
  ```yaml
  apiVersion: v1
  entries: wordpress:
  - annotations
    "azurecr.io/manifestDigest": sha256:08ef434162070bba4256414c80b001d15b7503ef2a1a4fa1f60bab174f80d4d7
  appVersion: 5.1.0
  created: "2019-03-06T16:59:25.8892193Z"
  ```

## Azure Container Registry User Guidance

Evaluating the information provided to the [Helm security advisories](https://github.com/helm/helm/security/advisories):

- [Repository index file allows for duplicates of the same chart entry](https://github.com/helm/helm/security/advisories/GHSA-jm56-5h66-w453)
- [Sanitizing plugin names](https://github.com/helm/helm/security/advisories/GHSA-m54r-vrmv-hw33)
- [plugin.yaml file allows for duplicate entries](https://github.com/helm/helm/security/advisories/GHSA-c52f-pq47-2r9j)

We can provide some guidance to avoid customers being stuck between the proverbial rock and hard place while we rollout a server side change to regenerate the `chart.yaml` files.

First, some scoping to this guidance:

- [ACR is a private registry](https://aka.ms/acr), providing [private repository storage of Helm charts](https://aka.ms/acr/helm-repos)
- ACR implements [Azure Security Benchmarks](https://docs.microsoft.com/en-us/azure/container-registry/security-baseline) minimizing man in the middle attacks addressed in the helm security issues.
- The customer controls access to their resource, and can monitor requests through [ACR Audit Logs](https://aka.ms/acr/audit-logs)
- The helm client being used to pull charts from ACR is scoped to only pulling charts from ACR, and not public locations outside the control of the customers environment

## Getting Unblocked with Existing Workflows

Based on the above guidance, we recognize that customers can not be blocked for their deployments and we provide the following guidance:

- Use a helm client <= `v3.3.1` to access ACR content ONLY
- Helm server deployed within kubernetes is unrelated
- Do NOT use the same client to pull public charts as the concerns noted in the security advisories may apply
- As ACR completes the rollout tracked [here](https://aka.ms/acr/advisories), update to `v3.3.2` or newer tested clients.
- Once the rollout is complete, users will need to push any chart to their repository, trigger a regeneration of the `chart.yaml` file, conforming to new helm behavior

## Go Forward Plan

- [ACR & Helm Roadmap](https://github.com/Azure/acr/blob/main/docs/acr-roadmap.md#acr-helm-ga): Consider moving to [`helm registry`](https://helm.sh/docs/topics/registries/) support, which uses OCI Artifact to persist charts as all other artifacts in a registry.  
While there are gaps that typically apply to public repositories, the majority of requirements are covered for private deployments.
- ACR customers will benefit from [Private Link](https://aka.ms/acr/privatelink), [Auto-purge](https://aka.ms/acr/auto-purge) with enhancements coming this fall, on-prem registries and more
- The Helm community is [establishing a security notification process](https://github.com/helm/community/issues/128). Please weigh into the discussion with your thoughts and concerns.
- Consider importing public content to public registries, where you can security scan, test and verify the components you depend on work in your environment.  
This includes public charts, images and binaries like the helm client.  
See guidance on [Consuming Upstream Content in Your Software or Service](https://stevelasker.blog/2020/09/01/consuming-upstream-content/)

## What Is the ACR Bug

As noted in [Helm repo add fails with Azure Container Registry #8761](https://github.com/helm/helm/issues/8761), ACR is writing data to the chart for tracking, but it's no longer considered valid.

Why you might ask? - good question:

ACR `helm repo` was an initial experiment that persists helm charts within the Azure Container Registry infrastructure, providing all the production security, reliability, performance and sovereignty capabilities of ACR, to helm charts. As helm charts are stored within a registry, as a content addressable blob, we needed a means to store the content digest information. The example below shows wordpress being stored with it's digest.

```yaml
apiVersion: v1
entries:
 wordpress:
 - acrMetadata:
     manifestDigest: sha256:08ef434162070bba4256414c80b001d15b7503ef2a1a4fa1f60bab174f80d4d7
   appVersion: 5.1.0
   created: "2019-03-06T16:59:25.8892193Z"
```

The information isn't required by the client, but it's stored in the `chart.yaml` as it worked, and it solved the need. Prior to `helm v3.3.1` string elements were supported. The result of a security audit triggered the helm team to implement string yaml validation. We fully support the security fix and recognize the gap in security notifications between the helm project and consumers.

### The Fix

- The ACR service will change the `chart.yaml` formatting of the digest to conform to helm annotations:
  ```yaml
  apiVersion: v1
  entries: wordpress:
  - annotations
    "azurecr.io/manifestDigest": sha256:08ef434162070bba4256414c80b001d15b7503ef2a1a4fa1f60bab174f80d4d7
  appVersion: 5.1.0
  created: "2019-03-06T16:59:25.8892193Z"
  ```
- To trigger a server side regeneration of the `chart.yaml` a new chart will need to be pushed to acr
  ```shell
  helm create test
  helm package ./test
  az acr helm push ./test-0.1.0.tgz
  ```
- Once a push is complete, the `chart.yaml` will be updated to conform to the new annotations format
- The ACR Helm update must be completed in the region hosting the registry for the new format to be generated
- Once complete, all helm clients will continue to function, including new helm clients > `v3.3.1`

## Q&A

- **Q: Why was this security fix rolled out before a mitigation was in place?**
- **A:** There was no formal process in place for the Helm project to communicate security fixes to its consumers, nor did ACR confirm there was a security notification in process before supporting this capability. This is a good lesson for all consumers of open source projects to evaluate.
- **Q: Can ACR update my registry so I don't have push a chart to get the new format?**
- **A:** We evaluated back filling, however this would have taken longer to verify and implied higher risk as the number of registries supporting `helm repo` worldwide is substantial
- **Q: Am I secure in using helm < `v3.3.1` in production, even though there are security advisories?**
- **A:** Security should always be a concern. If you follow the above guidance for using `helm repo` uniquely with ACR, where you control all the content which was securely pushed by members of your trusted circle, you should be comfortable proceeding. The known security issues involve compromised registries, or shared public registries that share a single `chart.yaml`
- **Q: How can I secure my helm chart usage?**
- **A:** Consuming public content is an advantage and a risk. We recommend importing all public content and tooling being used within your organization. Security scan it, test each updated version and automate the process to assure you are always working with recent content, that is both secured and tested for your environment.  
See guidance on [Consuming Upstream Content in Your Software or Service](https://stevelasker.blog/2020/09/01/consuming-upstream-content/)

Please see [Azure Container Registry feedback & support](https://aka.ms/acr/links?#providing-feedback) for additional information.
