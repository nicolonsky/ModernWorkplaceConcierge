using Microsoft.Graph;
using Newtonsoft.Json;

namespace IntuneConcierge.Helpers
{
    public class DeviceManagementScripts
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }

        [JsonProperty("value")]
        public DeviceManagementScript[] value { get; set; }
    }
}