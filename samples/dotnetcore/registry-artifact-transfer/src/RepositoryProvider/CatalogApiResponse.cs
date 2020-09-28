using System.Collections.Generic;
using Newtonsoft.Json;

namespace RegistryArtifactTransfer
{
    public class CatalogApiResponse
    {
        [JsonProperty(PropertyName = "repositories")]
        public List<string> Repositories;
    }
}
