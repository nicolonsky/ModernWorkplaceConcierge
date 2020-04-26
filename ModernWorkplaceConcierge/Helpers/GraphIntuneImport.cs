using Microsoft.Graph;
using ModernWorkplaceConcierge.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphIntuneImport
    {
        private GraphIntune graphIntune;
        private SignalRMessage signalRMessage;
        private OverwriteBehaviour overwriteBehaviour;
        private IEnumerable<DeviceCompliancePolicy> compliancePolicies;
        private IEnumerable<DeviceConfiguration> deviceConfigurations;

        public GraphIntuneImport(string clientId, OverwriteBehaviour overwriteBehaviour)
        {
            this.signalRMessage = new SignalRMessage(clientId);
            this.graphIntune = new GraphIntune(clientId);
            this.overwriteBehaviour = overwriteBehaviour;
        }

        public async Task AddIntuneConfig(string result)
        {
            GraphJson json = JsonConvert.DeserializeObject<GraphJson>(result);

            switch (json.OdataValue)
            {
                case string odataValue when odataValue.Contains("CompliancePolicy"):

                    if (overwriteBehaviour != OverwriteBehaviour.IMPORT_AS_DUPLICATE && compliancePolicies == null)
                    {
                        this.compliancePolicies = await graphIntune.GetDeviceCompliancePoliciesAsync();
                    }

                    JObject o = JObject.Parse(result);
                    JObject o2 = JObject.Parse(@"{scheduledActionsForRule:[{ruleName:'PasswordRequired',scheduledActionConfigurations:[{actionType:'block',gracePeriodHours:'0',notificationTemplateId:'',notificationMessageCCList:[]}]}]}");
                    o.Add("scheduledActionsForRule", o2.SelectToken("scheduledActionsForRule"));
                    string jsonPolicy = JsonConvert.SerializeObject(o);

                    DeviceCompliancePolicy deviceCompliancePolicy = JsonConvert.DeserializeObject<DeviceCompliancePolicy>(jsonPolicy);

                    switch (overwriteBehaviour)
                    {
                        case OverwriteBehaviour.DISCARD:
                            if (!compliancePolicies.Any(p => p.Id.Contains(deviceCompliancePolicy.Id)))
                            {
                                var response = await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);
                                signalRMessage.sendMessage($"Success: added {response.ODataType} '{response.DisplayName}'");
                            }
                            else
                            {
                                signalRMessage.sendMessage($"Discarding Policy '{deviceCompliancePolicy.DisplayName}' ({deviceCompliancePolicy.DisplayName}) already exists!");
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            var deviceCompliancePolicyResponse2 = await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);
                            signalRMessage.sendMessage($"Success: added {deviceCompliancePolicyResponse2.ODataType} '{deviceCompliancePolicyResponse2.DisplayName}'");
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_ID:

                            // match by object ID
                            if (compliancePolicies.Any(p => p.Id.Contains(deviceCompliancePolicy.Id)))
                            {
                                deviceCompliancePolicy.ScheduledActionsForRule = null;
                                await graphIntune.PatchDeviceCompliancePolicyAsync(deviceCompliancePolicy);
                            }
                            // Create a new policy
                            else
                            {
                                var deviceCompliancePolicyResponse3 = await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);
                                signalRMessage.sendMessage($"Success: added {deviceCompliancePolicyResponse3.ODataType} '{deviceCompliancePolicyResponse3.DisplayName}'");
                            }
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_NAME:
                            if (compliancePolicies.Any(policy => policy.DisplayName.Equals(deviceCompliancePolicy.DisplayName)))
                            {
                                deviceCompliancePolicy.ScheduledActionsForRule = null;
                                string replaceObjectId = compliancePolicies.Where(policy => policy.DisplayName.Equals(deviceCompliancePolicy.DisplayName)).Select(policy => policy.Id).First();
                                deviceCompliancePolicy.Id = replaceObjectId;
                                await graphIntune.PatchDeviceCompliancePolicyAsync(deviceCompliancePolicy);
                            }
                            else
                            {
                                var deviceCompliancePolicyResponse4 = await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);
                                signalRMessage.sendMessage($"Success: added {deviceCompliancePolicyResponse4.ODataType} '{deviceCompliancePolicyResponse4.DisplayName}'");
                            }
                            break;
                    }
                    break;

                case string odataValue when odataValue.Contains("Configuration") && odataValue.Contains("windows"):

                    DeviceConfiguration deviceConfiguration = JsonConvert.DeserializeObject<DeviceConfiguration>(result);
                    // request fails when true
                    deviceConfiguration.SupportsScopeTags = null;
                    deviceConfiguration.RoleScopeTagIds = null;

                    string temp = JsonConvert.SerializeObject(deviceConfiguration);

                    

                    if (overwriteBehaviour != OverwriteBehaviour.IMPORT_AS_DUPLICATE && deviceConfigurations == null)
                    {
                        deviceConfigurations = await graphIntune.GetDeviceConfigurationsAsync();
                    }

                    switch (overwriteBehaviour)
                    {
                        case OverwriteBehaviour.DISCARD:
                            if (!deviceConfigurations.Any(p => p.Id.Contains(deviceConfiguration.Id)))
                            {
                                var response = await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);
                                signalRMessage.sendMessage($"Success: added {response.ODataType} '{response.DisplayName}'");
                            }
                            else
                            {
                                signalRMessage.sendMessage($"Discarding Policy '{deviceConfiguration.DisplayName}' ({deviceConfiguration.DisplayName}) already exists!");
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            var deviceCompliancePolicyResponse2 = await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);
                            signalRMessage.sendMessage($"Success: added {deviceCompliancePolicyResponse2.ODataType} '{deviceCompliancePolicyResponse2.DisplayName}'");
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_ID:

                            // match by object ID
                            if (deviceConfigurations.Any(p => p.Id.Contains(deviceConfiguration.Id)))
                            {
                                await graphIntune.PatchDeviceConfigurationAsync(deviceConfiguration);
                            }
                            // Create a new policy
                            else
                            {
                                var deviceCompliancePolicyResponse3 = await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);
                                signalRMessage.sendMessage($"Success: added {deviceCompliancePolicyResponse3.ODataType} '{deviceCompliancePolicyResponse3.DisplayName}'");
                            }
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_NAME:
                            if (deviceConfigurations.Any(policy => policy.DisplayName.Equals(deviceConfiguration.DisplayName)))
                            {
                                string replaceObjectId = deviceConfigurations.Where(policy => policy.DisplayName.Equals(deviceConfiguration.DisplayName)).Select(policy => policy.Id).First();
                                deviceConfiguration.Id = replaceObjectId;
                                await graphIntune.PatchDeviceConfigurationAsync(deviceConfiguration);
                            }
                            else
                            {
                                var deviceCompliancePolicyResponse4 = await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);
                                signalRMessage.sendMessage($"Success: added {deviceCompliancePolicyResponse4.ODataType} '{deviceCompliancePolicyResponse4.DisplayName}'");
                            }
                            break;
                    }
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