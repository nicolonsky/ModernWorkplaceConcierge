using Newtonsoft.Json;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphJson
    {
        [JsonProperty("@odata.type", NullValueHandling = NullValueHandling.Ignore)]
        public string OdataType { get; set; }

        [JsonProperty("@odata.context", NullValueHandling = NullValueHandling.Ignore)]
        public string OdataValue { get { return OdataType; } set { OdataType = value; } }
    }
}