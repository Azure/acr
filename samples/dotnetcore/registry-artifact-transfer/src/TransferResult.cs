using System.Collections.Generic;

namespace RegistryArtifactTransfer
{
    public class TransferResult
    {
        public List<string> Succeeded { get; set; } = new List<string>();
        public List<string> Failed { get; set; }  = new List<string>();
    }
}
