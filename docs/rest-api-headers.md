
# Azure Container Registry HTTP headers

Azure container registries are compatible with a multitude of services and orchestrators. To make it easier to track the source services and agents from which ACR is used, we have started using the `HttpHeaders` field in the Docker `config.json` file.

## Header format

The ACR headers follow this format:

```HTTP
X-Meta-Source-Client: <cloud>/<service>/<optionalservicename>
```

* `cloud`: Azure, Azure Stack, or other government- or country-specific Azure cloud. Although Azure Stack and government clouds are not currently supported, this parameter enables future support.
* `service`: The name of the service.
* `optionalservicename`: An optional parameter for services with subservices, or for specifying a SKU. For example, Web Apps corresponds to `azure/app-service/web-apps`).

## Header values

Partner services and orchestrators are encouraged to use specific header values to help with our telemetry. Users can also modify the value passed to the header if they so desire.

The values we ask ACR partners to use when populating the `X-Meta-Source-Client` field are as follows:

| Service Name              | Header                                |
| ------------------------- | ------------------------------------- |
| Azure Container Service   | `azure/compute/azure-container-service` |
| App Service - Web Apps    | `azure/app-service/web-apps`            |
| App Service - Logic Apps  | `azure/app-service/logic-apps`          |
| Batch                     | `azure/compute/batch`                   |
| Cloud Console             | `azure/cloud-console`                   |
| Functions                 | `azure/compute/functions`               |
| Internet of Things - Hub  | `azure/iot/hub`                         |
| HDInsight                 | `azure/data/hdinsight`                  |
| Jenkins                   | `azure/jenkins`                         |
| Machine Learning          | `azure/data/machile-learning`           |
| Service Fabric            | `azure/compute/service-fabric`          |
| VSTS                      | `azure/vsts`                            |
