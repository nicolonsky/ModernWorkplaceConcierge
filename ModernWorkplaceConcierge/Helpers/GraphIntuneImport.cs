using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphIntuneImport
    {
        private GraphIntune graphIntune;
        private SignalRMessage signalRMessage;

        public GraphIntuneImport(string clientId)
        {
            this.signalRMessage = new SignalRMessage(clientId);
            this.graphIntune = new GraphIntune(clientId);
        }

        public async Task AddIntuneConfig(string result)
        {
            GraphJson json = JsonConvert.DeserializeObject<GraphJson>(result);

            switch (json.OdataValue)
            {
                case string odataValue when odataValue.Contains("CompliancePolicy"):
                    JObject o = JObject.Parse(result);
                    JObject o2 = JObject.Parse(@"{scheduledActionsForRule:[{ruleName:'PasswordRequired',scheduledActionConfigurations:[{actionType:'block',gracePeriodHours:'0',notificationTemplateId:'',notificationMessageCCList:[]}]}]}");
                    o.Add("scheduledActionsForRule", o2.SelectToken("scheduledActionsForRule"));
                    string jsonPolicy = JsonConvert.SerializeObject(o);

                    DeviceCompliancePolicy deviceCompliancePolicy = JsonConvert.DeserializeObject<DeviceCompliancePolicy>(jsonPolicy);
                    var response = await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);
                    signalRMessage.sendMessage($"Success: added {response.ODataType} '{response.DisplayName}'");
                    break;

                case string odataValue when odataValue.Contains("Configuration") && odataValue.Contains("windows"):
                    DeviceConfiguration deviceConfiguration = JsonConvert.DeserializeObject<DeviceConfiguration>(result);
                    // request fails when true
                    deviceConfiguration.SupportsScopeTags = false;
                    var response1 = await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);
                    signalRMessage.sendMessage($"Success: added {response1.ODataType} '{response1.DisplayName}'");
                    break;

                case string odataValue when odataValue.Contains("deviceManagementScripts"):
                    DeviceManagementScript deviceManagementScript = JsonConvert.DeserializeObject<DeviceManagementScript>(result);
                    // remove id - otherwise request fails
                    deviceManagementScript.Id = "";
                    var response2 = await graphIntune.AddDeviceManagementScriptsAsync(deviceManagementScript);
                    signalRMessage.sendMessage($"Success: added {response2.ODataType} '{response2.DisplayName}'");
                    break;

                case string odataValue when odataValue.Contains("WindowsAutopilotDeploymentProfile"):
                    WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = JsonConvert.DeserializeObject<WindowsAutopilotDeploymentProfile>(result);
                    var response3 = await graphIntune.AddWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);
                    signalRMessage.sendMessage($"Success: added {response3.ODataType} '{response3.DisplayName}'");
                    break;

                case string odataValue when odataValue.Contains("#microsoft.graph.iosManagedAppProtection"):
                    var response4 = await graphIntune.ImportIosManagedAppProtectionAsync(result);
                    signalRMessage.sendMessage($"Success: added {response4.ODataType} '{response4.DisplayName}'");
                    break;

                case string odataValue when odataValue.Contains("#microsoft.graph.androidManagedAppProtection"):
                    var response5 = await graphIntune.ImportAndroidManagedAppProtectionAsync(result);
                    signalRMessage.sendMessage($"Success: added {response5.ODataType} '{response5.DisplayName}'");
                    break;

                case string odataValue when odataValue.Contains("#microsoft.graph.targetedManagedAppConfiguration"):
                    var response6 = await graphIntune.ImportWindowsManagedAppProtectionAsync(result);
                    signalRMessage.sendMessage($"Success: added {response6.ODataType} '{response6.DisplayName}'");
                    break;
            }
        }
    }
}