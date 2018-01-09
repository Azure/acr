
# Azure Container Registry HTTP headers

Azure container registries are compatible with a multitude of services and orchestrators. To help our customers, we'd like to understand which services in Azure, or outside of Azure, are issuing registry requests. To track the source services and agents from which ACR is used, we have started using the `HttpHeaders` field in the Docker `config.json` file.

## Header format

ACR will parse headers using the following format:

```HTTP
X-Meta-Source-Client: <cloud>/<service>/<optionalservicename>
```

* `cloud`: Azure, Azure Stack, or other government- or country-specific Azure cloud.
* `service`: The name of the service.
* `optionalservicename`: An optional parameter for services with subservices, or for specifying a SKU. For example, Web Apps corresponds to `azure/app-service/web-apps`).

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

| Cloud                     | Header                                  |
| ------------------------- | --------------------------------------- |
| Azure Public Cloud        | `azure/`                                |
| Azure Stack               | `azurestack/`                           |
| China (Mooncake)          | `china/`                                |
| Germany                   | `germany/`                              |
| US Gov                    | `auzreusgov/`                           |
| US DOD                    | `auzreusdod/`                           |


| Service name              | Header                                  |
| ------------------------- | --------------------------------------- |
| App Service - Logic Apps  | `azure/app-service/logic-apps`          |
| App Service - Web Apps    | `azure/app-service/web-apps`            |
| Azure Container Instance  | `azure/aci`                             |
| Azure Container Service   | `azure/acs`                             |
| Azure Kubernetes Service  | `azure/aks`                             |
| Azure Container Builder   | `azure/acb`                             |
| Batch                     | `azure/batch`                           |
| Cloud Console             | `azure/cloud-console`                   |
| Functions                 | `azure/functions`                       |
| HDInsight                 | `azure/hdinsight`                       |
| Internet of Things - Hub  | `azure/iot/hub`                         |
| Jenkins                   | `azure/jenkins`                         |
| Machine Learning          | `azure/ml`                              |
| Service Fabric            | `azure/service-fabric`                  |
| VSTS                      | `azure/vsts`                            |
