using System.Collections.Generic;
using Newtonsoft.Json;

namespace RegistryArtifactTransfer
{
    public class TagListApiResponse
    {
        [JsonProperty(PropertyName = "name")]
        public string Name;

        [JsonProperty(PropertyName = "tags")]
        public List<string> Tags;
    }
}
