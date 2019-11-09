using System;
using Newtonsoft.Json;
using Microsoft.Graph;


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