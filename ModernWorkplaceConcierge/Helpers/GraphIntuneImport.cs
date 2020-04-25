using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ModernWorkplaceConcierge.TokenStorage;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Net.Http;
using ModernWorkplaceConcierge.Helpers;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections;
using Microsoft.AspNet.SignalR;
using System.IO;
using ModernWorkplaceConcierge.Models;
using System;
using System.Web.Http.Results;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphIntuneImport
    {
        private SignalRMessage signalRMessage;
        private GraphIntune graphIntune;

        public GraphIntuneImport(string clientId)
        {
            this.signalRMessage = new SignalRMessage(clientId);
            this.graphIntune = new GraphIntune(clientId);
        }

        public async Task<string> AddIntuneConfig(string result, string clientId = null)
        {
            GraphJson json = JsonConvert.DeserializeObject<GraphJson>(result);

            if (json.OdataValue.Contains("CompliancePolicy"))
            {
                JObject o = JObject.Parse(result);

                JObject o2 = JObject.Parse(@"{scheduledActionsForRule:[{ruleName:'PasswordRequired',scheduledActionConfigurations:[{actionType:'block',gracePeriodHours:'0',notificationTemplateId:'',notificationMessageCCList:[]}]}]}");

                o.Add("scheduledActionsForRule", o2.SelectToken("scheduledActionsForRule"));

                string jsonPolicy = JsonConvert.SerializeObject(o);

                DeviceCompliancePolicy deviceCompliancePolicy = JsonConvert.DeserializeObject<DeviceCompliancePolicy>(jsonPolicy);

                var response = await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);

                return response.ODataType + " | " + response.DisplayName;
            }
            else if (json.OdataValue.Contains("Configuration") && json.OdataValue.Contains("windows"))
            {
                DeviceConfiguration deviceConfiguration = JsonConvert.DeserializeObject<DeviceConfiguration>(result);

                // request fails when true :(
                deviceConfiguration.SupportsScopeTags = false;

                var response = await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);

                return response.ODataType + " | " + response.DisplayName;
            }
            else if (json.OdataValue.Contains("deviceManagementScripts"))
            {
                DeviceManagementScript deviceManagementScript = JsonConvert.DeserializeObject<DeviceManagementScript>(result);

                // remove id - otherwise request fails
                deviceManagementScript.Id = "";

                var response = await graphIntune.AddDeviceManagementScriptsAsync(deviceManagementScript);

                return "#microsoft.graph.deviceManagementScript" + " | " + response.DisplayName;
            }
            else if (json.OdataValue.Contains("WindowsAutopilotDeploymentProfile"))
            {
                WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = JsonConvert.DeserializeObject<WindowsAutopilotDeploymentProfile>(result);

                var response = await graphIntune.AddWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);

                return response.ODataType + " | " + response.DisplayName;

            }
            else if (json.OdataValue.Contains("#microsoft.graph.iosManagedAppProtection"))
            {
                var response = await graphIntune.ImportIosManagedAppProtectionAsync(result);
                return "#microsoft.graph.iosManagedAppProtection | " + response.DisplayName;

            }
            else if (json.OdataValue.Contains("#microsoft.graph.androidManagedAppProtection"))
            { 
                var response = await graphIntune.ImportAndroidManagedAppProtectionAsync(result);
                return "#microsoft.graph.androidManagedAppProtection | " + response.DisplayName;
            }
            else if (json.OdataValue.Contains("#microsoft.graph.targetedManagedAppConfiguration"))
            {
                var response = await graphIntune.ImportWindowsManagedAppProtectionAsync(result);
                return "#microsoft.graph.targetedManagedAppConfiguration | " + response.DisplayName;
            }
            else
            {
                return null;
            }
        }
    }
}