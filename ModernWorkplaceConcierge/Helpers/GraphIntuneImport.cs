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
        private IEnumerable<DeviceCompliancePolicy> compliancePolicies;
        private IEnumerable<DeviceConfiguration> deviceConfigurations;
        private IEnumerable<DeviceManagementScript> deviceManagementScipts;
        private IEnumerable<WindowsAutopilotDeploymentProfile> windowsAutopilotDeploymentProfiles;
        private IEnumerable<IosManagedAppProtection> iosManagedAppProtections;
        private IEnumerable<AndroidManagedAppProtection> androidManagedAppProtections;
        private IEnumerable<TargetedManagedAppConfiguration> targetedManagedAppConfigurations;
        private IEnumerable<ManagedDeviceMobileAppConfiguration> managedDeviceMobileAppConfigurations;
        private GraphIntune graphIntune;
        private OverwriteBehaviour overwriteBehaviour;
        private SignalRMessage signalRMessage;
        private List<string> supportedDeviceConfigurations = new List<string>();


        public GraphIntuneImport(string clientId, OverwriteBehaviour overwriteBehaviour)
        {
            this.signalRMessage = new SignalRMessage(clientId);
            this.graphIntune = new GraphIntune(clientId);
            this.overwriteBehaviour = overwriteBehaviour;
        }

        public async Task AddIntuneConfig(string result)
        {
            supportedDeviceConfigurations.Add("#microsoft.graph.windows10GeneralConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.androidDeviceOwnerGeneralDeviceConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.windowsUpdateForBusinessConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.windows10EndpointProtectionConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.windows10CustomConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.iosGeneralDeviceConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.androidWorkProfileGeneralDeviceConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.iosEnterpriseWiFiConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.androidDeviceOwnerEnterpriseWiFiConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.androidDeviceOwnerWiFiConfiguration");
            supportedDeviceConfigurations.Add("#microsoft.graph.windowsDeliveryOptimizationConfiguration");

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
                            if (compliancePolicies.All(p => !p.Id.Contains(deviceCompliancePolicy.Id)) && compliancePolicies.All(p => !p.DisplayName.Contains(deviceCompliancePolicy.DisplayName)))
                            {
                                await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);
                            }
                            else
                            {
                                if (compliancePolicies.Any(p => p.Id.Contains(deviceCompliancePolicy.Id)))
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{deviceCompliancePolicy.DisplayName}' ({deviceCompliancePolicy.Id}) already exists!");
                                }
                                else
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{deviceCompliancePolicy.DisplayName}' - configuration with this name already exists!");
                                }
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);
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
                                await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);
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
                                await graphIntune.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);
                            }
                            break;
                    }
                    break;

                case string odataValue when supportedDeviceConfigurations.Contains(odataValue):

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
                            if (deviceConfigurations.All(p => !p.Id.Contains(deviceConfiguration.Id)) && deviceConfigurations.All(p => !p.DisplayName.Contains(deviceConfiguration.DisplayName)))
                            {
                                await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);
                            }
                            else
                            {
                                if (deviceConfigurations.Any(p => p.Id.Contains(deviceConfiguration.Id)))
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{deviceConfiguration.DisplayName}' ({deviceConfiguration.Id}) already exists!");
                                }
                                else
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{deviceConfiguration.DisplayName}' - configuration with this name already exists!");
                                }
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);
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
                                await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);
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
                                await graphIntune.AddDeviceConfigurationAsync(deviceConfiguration);
                            }
                            break;
                    }
                    break;

                case string odataValue when odataValue.Contains("deviceManagementScripts"):
                    DeviceManagementScript deviceManagementScript = JsonConvert.DeserializeObject<DeviceManagementScript>(result);
                   

                    if (overwriteBehaviour != OverwriteBehaviour.IMPORT_AS_DUPLICATE && deviceManagementScipts == null)
                    {
                        deviceManagementScipts =  await graphIntune.GetDeviceManagementScriptsAsync();
                    }

                    switch (overwriteBehaviour)
                    {
                        case OverwriteBehaviour.DISCARD:
                            if (deviceManagementScipts.All(p => !p.Id.Contains(deviceManagementScript.Id)) && deviceManagementScipts.All(p => !p.DisplayName.Contains(deviceManagementScript.DisplayName)))
                            {
                                await graphIntune.AddDeviceManagementScriptAsync(deviceManagementScript);
                            }
                            else
                            {
                                if (deviceManagementScipts.Any(p => p.Id.Contains(deviceManagementScript.Id)))
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{deviceManagementScript.DisplayName}' ({deviceManagementScript.Id}) already exists!");
                                }
                                else
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{deviceManagementScript.DisplayName}' - configuration with this name already exists!");
                                }
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            await graphIntune.AddDeviceManagementScriptAsync(deviceManagementScript);
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_ID:

                            // match by object ID
                            if (deviceManagementScipts.Any(p => p.Id.Contains(deviceManagementScript.Id)))
                            {
                                await graphIntune.PatchDeviceManagementScriptAsync(deviceManagementScript);
                            }
                            // Create a new policy
                            else
                            {
                                await graphIntune.AddDeviceManagementScriptAsync(deviceManagementScript);
                            }
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_NAME:
                            if (deviceManagementScipts.Any(policy => policy.DisplayName.Equals(deviceManagementScript.DisplayName)))
                            {
                                string replaceObjectId = deviceManagementScipts.Where(policy => policy.DisplayName.Equals(deviceManagementScript.DisplayName)).Select(policy => policy.Id).First();
                                deviceManagementScript.Id = replaceObjectId;
                                await graphIntune.PatchDeviceManagementScriptAsync(deviceManagementScript);
                            }
                            else
                            {
                                await graphIntune.AddDeviceManagementScriptAsync(deviceManagementScript);
                            }
                            break;
                    }
                    break;

                case string odataValue when odataValue.Contains("WindowsAutopilotDeploymentProfile"):
                    WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = JsonConvert.DeserializeObject<WindowsAutopilotDeploymentProfile>(result);

                    if (overwriteBehaviour != OverwriteBehaviour.IMPORT_AS_DUPLICATE && windowsAutopilotDeploymentProfiles == null)
                    {
                        windowsAutopilotDeploymentProfiles = await graphIntune.GetWindowsAutopilotDeploymentProfiles();
                    }

                    switch (overwriteBehaviour)
                    {
                        case OverwriteBehaviour.DISCARD:
                            if (windowsAutopilotDeploymentProfiles.All(p => !p.Id.Contains(windowsAutopilotDeploymentProfile.Id)) && windowsAutopilotDeploymentProfiles.All(p => !p.DisplayName.Contains(windowsAutopilotDeploymentProfile.DisplayName)))
                            {
                                await graphIntune.AddWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);
                            }
                            else
                            {
                                if (windowsAutopilotDeploymentProfiles.Any(p => p.Id.Contains(windowsAutopilotDeploymentProfile.Id)))
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{windowsAutopilotDeploymentProfile.DisplayName}' ({windowsAutopilotDeploymentProfile.Id}) already exists!");
                                }
                                else
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{windowsAutopilotDeploymentProfile.DisplayName}' - configuration with this name already exists!");
                                }
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            await graphIntune.AddWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_ID:

                            // match by object ID
                            if (windowsAutopilotDeploymentProfiles.Any(p => p.Id.Contains(windowsAutopilotDeploymentProfile.Id)))
                            {
                                await graphIntune.PatchWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);
                            }
                            // Create a new policy
                            else
                            {
                                await graphIntune.AddWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);
                            }
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_NAME:
                            if (windowsAutopilotDeploymentProfiles.Any(policy => policy.DisplayName.Equals(windowsAutopilotDeploymentProfile.DisplayName)))
                            {
                                string replaceObjectId = windowsAutopilotDeploymentProfiles.Where(policy => policy.DisplayName.Equals(windowsAutopilotDeploymentProfile.DisplayName)).Select(policy => policy.Id).First();
                                windowsAutopilotDeploymentProfile.Id = replaceObjectId;
                                await graphIntune.PatchWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);
                            }
                            else
                            {
                                await graphIntune.AddWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);
                            }
                            break;
                    }
                    break;

                case string odataValue when odataValue.Contains("#microsoft.graph.iosManagedAppProtection"):
                    IosManagedAppProtection iosManagedAppProtection = JsonConvert.DeserializeObject<IosManagedAppProtection>(result);

                    if (overwriteBehaviour != OverwriteBehaviour.IMPORT_AS_DUPLICATE && iosManagedAppProtections == null)
                    {
                        iosManagedAppProtections = await graphIntune.GetIosManagedAppProtectionsAsync();
                    }

                    switch (overwriteBehaviour)
                    {
                        case OverwriteBehaviour.DISCARD:
                            if (iosManagedAppProtections.All(p => !p.Id.Contains(iosManagedAppProtection.Id)) && iosManagedAppProtections.All(p => !p.DisplayName.Contains(iosManagedAppProtection.DisplayName)))
                            {
                                await graphIntune.ImportIosManagedAppProtectionAsync(result);
                            }
                            else
                            {
                                if (iosManagedAppProtections.Any(p => p.Id.Contains(iosManagedAppProtection.Id)))
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{iosManagedAppProtection.DisplayName}' ({iosManagedAppProtection.Id}) already exists!");
                                }
                                else
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{iosManagedAppProtection.DisplayName}' - configuration with this name already exists!");
                                }
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            await graphIntune.ImportIosManagedAppProtectionAsync(result);
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_ID:

                            // match by object ID
                            if (iosManagedAppProtections.Any(p => p.Id.Contains(iosManagedAppProtection.Id)))
                            {
                                await graphIntune.ImportPatchIosManagedAppProtectionAsync(result);
                            }
                            // Create a new policy
                            else
                            {
                                await graphIntune.ImportIosManagedAppProtectionAsync(result);
                            }
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_NAME:
                            if (iosManagedAppProtections.Any(policy => policy.DisplayName.Equals(iosManagedAppProtection.DisplayName)))
                            {
                                string replaceObjectId = iosManagedAppProtections.Where(policy => policy.DisplayName.Equals(iosManagedAppProtection.DisplayName)).Select(policy => policy.Id).First();
                                // Replace id in json file
                                JObject jObject = JObject.Parse(result);
                                jObject.SelectToken("id").Replace(replaceObjectId);

                                await graphIntune.ImportPatchIosManagedAppProtectionAsync(jObject.ToString());
                            }
                            else
                            {
                                await graphIntune.ImportIosManagedAppProtectionAsync(result);
                            }
                            break;
                    }

                    break;

                case string odataValue when odataValue.Contains("#microsoft.graph.androidManagedAppProtection"):

                    AndroidManagedAppProtection androidManagedAppProtection = JsonConvert.DeserializeObject<AndroidManagedAppProtection>(result);

                    if (overwriteBehaviour != OverwriteBehaviour.IMPORT_AS_DUPLICATE && androidManagedAppProtections == null)
                    {
                        androidManagedAppProtections = await graphIntune.GetAndroidManagedAppProtectionsAsync();
                    }

                    switch (overwriteBehaviour)
                    {
                        case OverwriteBehaviour.DISCARD:
                            if (androidManagedAppProtections.All(p => !p.Id.Contains(androidManagedAppProtection.Id)) && androidManagedAppProtections.All(p => !p.DisplayName.Contains(androidManagedAppProtection.DisplayName)))
                            {
                                await graphIntune.ImportAndroidManagedAppProtectionAsync(result);
                            }
                            else
                            {
                                if (androidManagedAppProtections.Any(p => p.Id.Contains(androidManagedAppProtection.Id)))
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{androidManagedAppProtection.DisplayName}' ({androidManagedAppProtection.Id}) already exists!");
                                }
                                else
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{androidManagedAppProtection.DisplayName}' - configuration with this name already exists!");
                                }
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            await graphIntune.ImportAndroidManagedAppProtectionAsync(result);
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_ID:

                            // match by object ID
                            if (androidManagedAppProtections.Any(p => p.Id.Contains(androidManagedAppProtection.Id)))
                            {
                                await graphIntune.ImportPatchAndroidManagedAppProtectionAsync(result);
                            }
                            // Create a new policy
                            else
                            {
                                await graphIntune.ImportAndroidManagedAppProtectionAsync(result);
                            }
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_NAME:
                            if (androidManagedAppProtections.Any(policy => policy.DisplayName.Equals(androidManagedAppProtection.DisplayName)))
                            {
                                string replaceObjectId = androidManagedAppProtections.Where(policy => policy.DisplayName.Equals(androidManagedAppProtection.DisplayName)).Select(policy => policy.Id).First();
                                // Replace id in json file
                                JObject jObject = JObject.Parse(result);
                                jObject.SelectToken("id").Replace(replaceObjectId);

                                await graphIntune.ImportPatchAndroidManagedAppProtectionAsync(jObject.ToString());
                            }
                            else
                            {
                                await graphIntune.ImportAndroidManagedAppProtectionAsync(result);
                            }
                            break;
                    }
                    break;

                case string odataValue when odataValue.Contains("#microsoft.graph.targetedManagedAppConfiguration"):
                    TargetedManagedAppConfiguration targetedManagedAppConfiguration = JsonConvert.DeserializeObject<TargetedManagedAppConfiguration>(result);


                    if (overwriteBehaviour != OverwriteBehaviour.IMPORT_AS_DUPLICATE && targetedManagedAppConfigurations == null)
                    {
                        targetedManagedAppConfigurations =  await graphIntune.GetTargetedManagedAppConfigurationsAsync();
                    }


                    switch (overwriteBehaviour)
                    {
                        case OverwriteBehaviour.DISCARD:
                            if (targetedManagedAppConfigurations.All(p => !p.Id.Contains(targetedManagedAppConfiguration.Id)) && targetedManagedAppConfigurations.All(p => !p.DisplayName.Contains(targetedManagedAppConfiguration.DisplayName)))
                            {
                                await graphIntune.ImportTargetedManagedAppConfigurationAsync(result);
                            }
                            else
                            {
                                if (targetedManagedAppConfigurations.Any(p => p.Id.Contains(targetedManagedAppConfiguration.Id)))
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{targetedManagedAppConfiguration.DisplayName}' ({targetedManagedAppConfiguration.Id}) already exists!");
                                }
                                else
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{targetedManagedAppConfiguration.DisplayName}' - configuration with this name already exists!");
                                }
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            await graphIntune.ImportTargetedManagedAppConfigurationAsync(result);
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_ID:

                            // match by object ID
                            if (targetedManagedAppConfigurations.Any(p => p.Id.Contains(targetedManagedAppConfiguration.Id)))
                            {
                                await graphIntune.ImportPatchTargetedManagedAppConfigurationAsync(result);
                            }
                            // Create a new policy
                            else
                            {
                                await graphIntune.ImportTargetedManagedAppConfigurationAsync(result);
                            }
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_NAME:
                            if (targetedManagedAppConfigurations.Any(policy => policy.DisplayName.Equals(targetedManagedAppConfiguration.DisplayName)))
                            {
                                string replaceObjectId = targetedManagedAppConfigurations.Where(policy => policy.DisplayName.Equals(targetedManagedAppConfiguration.DisplayName)).Select(policy => policy.Id).First();
                                // Replace id in json file
                                JObject jObject = JObject.Parse(result);
                                jObject.SelectToken("id").Replace(replaceObjectId);

                                await graphIntune.ImportPatchTargetedManagedAppConfigurationAsync(jObject.ToString());
                            }
                            else
                            {
                                await graphIntune.ImportTargetedManagedAppConfigurationAsync(result);
                            }
                            break;
                    }
                    break;

                case string odataValue when odataValue.Contains("MobileAppConfiguration"):
                    ManagedDeviceMobileAppConfiguration managedDeviceMobileAppConfiguration = JsonConvert.DeserializeObject<ManagedDeviceMobileAppConfiguration>(result);

                    if (overwriteBehaviour != OverwriteBehaviour.IMPORT_AS_DUPLICATE && managedDeviceMobileAppConfigurations == null)
                    {
                        managedDeviceMobileAppConfigurations = await graphIntune.GetManagedDeviceMobileAppConfigurationsAsync();
                    }

                    switch (overwriteBehaviour)
                    {
                        case OverwriteBehaviour.DISCARD:
                            if (managedDeviceMobileAppConfigurations.All(p => !p.Id.Contains(managedDeviceMobileAppConfiguration.Id)) && managedDeviceMobileAppConfigurations.All(p => !p.DisplayName.Contains(managedDeviceMobileAppConfiguration.DisplayName)))
                            {
                                await graphIntune.AddManagedDeviceMobileAppConfigurationAsync(managedDeviceMobileAppConfiguration);
                            }
                            else
                            {
                                if (managedDeviceMobileAppConfigurations.Any(p => p.Id.Contains(managedDeviceMobileAppConfiguration.Id)))
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{managedDeviceMobileAppConfiguration.DisplayName}' ({managedDeviceMobileAppConfiguration.Id}) already exists!");
                                }
                                else
                                {
                                    signalRMessage.sendMessage($"Discarding configuration '{managedDeviceMobileAppConfiguration.DisplayName}' - configuration with this name already exists!");
                                }
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            await graphIntune.AddManagedDeviceMobileAppConfigurationAsync(managedDeviceMobileAppConfiguration);
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_ID:

                            // match by object ID
                            if (managedDeviceMobileAppConfigurations.Any(p => p.Id.Contains(managedDeviceMobileAppConfiguration.Id)))
                            {
                                await graphIntune.PatchManagedDeviceMobileAppConfigurationAsync(managedDeviceMobileAppConfiguration);
                            }
                            // Create a new policy
                            else
                            {
                                await graphIntune.AddManagedDeviceMobileAppConfigurationAsync(managedDeviceMobileAppConfiguration);
                            }
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_NAME:
                            if (managedDeviceMobileAppConfigurations.Any(policy => policy.DisplayName.Equals(managedDeviceMobileAppConfiguration.DisplayName)))
                            {
                                string replaceObjectId = managedDeviceMobileAppConfigurations.Where(policy => policy.DisplayName.Equals(managedDeviceMobileAppConfiguration.DisplayName)).Select(policy => policy.Id).First();
                                managedDeviceMobileAppConfiguration.Id = replaceObjectId;
                                await graphIntune.PatchManagedDeviceMobileAppConfigurationAsync(managedDeviceMobileAppConfiguration);
                            }
                            else
                            {
                                await graphIntune.AddManagedDeviceMobileAppConfigurationAsync(managedDeviceMobileAppConfiguration);
                            }
                            break;
                    }
                    break;
                case string odataValue when odataValue.Contains("#microsoft.graph.mdmWindowsInformationProtectionPolicy"):
                    MdmWindowsInformationProtectionPolicy windowsInformationProtection = JsonConvert.DeserializeObject<MdmWindowsInformationProtectionPolicy>(result);
                    await graphIntune.AddMdmWindowsInformationProtectionsAsync(windowsInformationProtection);
                    break;

                case string odataValue when odataValue.Contains("#microsoft.graph.windowsInformationProtectionPolicy"):
                    WindowsInformationProtectionPolicy windowsInformationProtectionUnmanaged = JsonConvert.DeserializeObject<WindowsInformationProtectionPolicy>(result);
                    await graphIntune.AddWindowsInformationProtectionsAsync(windowsInformationProtectionUnmanaged);
                    break;

                default:
                    throw new System.Exception($"Unsupported configuration type {json.OdataValue}");
            }
        }
    }
}