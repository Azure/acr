
# Azure Container Registry HTTP headers

Azure container registries are compatible with a multitude of services and orchestrators. To help our customers, we'd like to understand which services in Azure, or outside of Azure, are issuing registry requests. To track the source services and agents from which ACR is used, we have started using the `HttpHeaders` field in the Docker `config.json` file.

## Header format

ACR will parse headers using the following format:

```HTTP
X-Meta-Source-Client: <cloud>/<service>/<optionalservicename>
```

* `cloud`: Azure, Azure Stack, or other government- or country-specific Azure cloud.
* `service`: The name of the service.
* `optionalservicename`: An optional parameter for services with subservices, or for specifying a SKU. For example, Web Apps corresponds to `azure/app-service/web-apps`. The servicename can also be a hierarchy path, for example `azure/acr/connected-registry/instance-1`.

### Example

```JSON
{
	"HttpHeaders": {
		"X-Meta-Source-Client": "azure/aks"
	},
	"auths": {
		"myregistry.azurecr.io": {},
	},
	"credsStore": "wincred"
}
```

## Header values

Partner services and orchestrators are encouraged to use specific header values to help with our telemetry. Users can also modify the value passed to the header if they so desire.

The values we ask ACR partners to use when populating the `X-Meta-Source-Client` field are:

| Cloud              | Header        |
| ------------------ | ------------- |
| Azure Public Cloud | `azure/`      |
| Azure Stack        | `azurestack/` |
| China (Mooncake)   | `china/`      |
| Germany            | `germany/`    |
| US DOD             | `azureusdod/` |
| US Gov             | `azureusgov/` |
| On Premise         | `on-prem/`    |

| Service or Orchestrator name   | Header                                    |
| ------------------------------ | ----------------------------------------- |
| App Service - Logic Apps       | `azure/app-service/logic-apps`            |
| App Service - Web Apps         | `azure/app-service/web-apps`              |
| Azure Container Builder        | `azure/acb`                               |
| Azure Container Instance       | `azure/aci`                               |
| Azure Container Service        | `azure/acs`                               |
| Azure Kubernetes Service       | `azure/aks`                               |
| AKS Engine (Kubernetes)        | `azure/aks-engine`                        |
| Cluster API Azure (Kubernetes) | `azure/capz`                              |
| Batch                          | `azure/batch`                             |
| Cloud Console                  | `azure/cloud-console`                     |
| Functions                      | `azure/functions`                         |
| HDInsight                      | `azure/hdinsight`                         |
| Internet of Things - Hub       | `azure/iot/hub`                           |
| Jenkins                        | `azure/jenkins`                           |
| Machine Learning               | `azure/ml`                                |
| Service Fabric                 | `azure/service-fabric`                    |
| VSTS                           | `azure/vsts`                              |
| ACR Tasks                      | `azure/acr/tasks`                         |
| ACR Connected Registry         | `azure/acr/connected-registry/instance-1` |
| Microsoft Defender for Cloud - ACR scanner that pulls images for vulnerability assessment | `azure/mdc/scanner-svc-image-puller`      |
| Microsoft Defender for Cloud - ACR scanner for registry discovery and metadata            | `azure/mdc/scanner-svc-image-discovery`   |
| Microsoft Defender for Cloud - ACR scanner for container image enrichment                 | `azure/mdc/scanner-svc-image-enrichment`  |
| Microsoft Defender for Cloud - Azure DevOps CLI scanner that pulls images                 | `azure/mdc/scanner-ado-cli-image-puller`  |

## How ACR uses this header

The `X-Meta-Source-Client` header is a client-supplied, unauthenticated, and untrusted value. Both Microsoft and non-Microsoft clients can set or modify it freely, and ACR does not validate it during requests. ACR uses this header **only** for telemetry — traffic analysis, aggregation, and attribution of request sources to understand usage patterns. Specifically, ACR does **not** use this header for:

- Authentication or authorization
- Throttling, rate limiting, or quota calculations or exemptions
- Request routing or prioritization
- Any other business or control-plane logic

Likewise, anything that observes or consumes this traffic or its telemetry — service meshes, proxies, gateways, traffic analyzers, monitoring systems, and business analytics dashboards or reports — should not take a trusted dependency on this header's value, since it is self-reported by the client.
