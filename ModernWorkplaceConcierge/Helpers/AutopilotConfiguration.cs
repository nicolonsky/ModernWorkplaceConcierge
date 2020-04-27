using Microsoft.Graph;
using Newtonsoft.Json;
using System;

namespace ModernWorkplaceConcierge.Helpers
{
    public class CloudAssignedAadServerData
    {
        public ZeroTouchConfig ZeroTouchConfig;

        public CloudAssignedAadServerData(ZeroTouchConfig zeroTouchConfig)
        {
            this.ZeroTouchConfig = new ZeroTouchConfig(zeroTouchConfig.CloudAssignedTenantDomain, zeroTouchConfig.ForcedEnrollment);
        }
    }

    public class ZeroTouchConfig
    {
        public String CloudAssignedTenantUpn;
        public int ForcedEnrollment;
        public String CloudAssignedTenantDomain;

        public ZeroTouchConfig(String CloudAssignedTenantDomain, int ForcedEnrollment)
        {
            this.CloudAssignedTenantUpn = "";
            this.ForcedEnrollment = ForcedEnrollment;
            this.CloudAssignedTenantDomain = CloudAssignedTenantDomain;
        }
    }

    public class AutopilotConfiguration
    {
        //https://docs.microsoft.com/en-us/windows/deployment/windows-autopilot/existing-devices

        public String CloudAssignedTenantId;
        public String CloudAssignedDeviceName;
        public int CloudAssignedForcedEnrollment;
        public int Version;
        public String Comment_File;
        public string CloudAssignedAadServerData;
        public int CloudAssignedDomainJoinMethod;
        public int CloudAssignedOobeConfig;
        public String ZtdCorrelationId;
        public String CloudAssignedTenantDomain;
        public String CloudAssignedLanguage;

        public AutopilotConfiguration(Microsoft.Graph.WindowsAutopilotDeploymentProfile profile, Microsoft.Graph.Organization organization)
        {
            Comment_File = "Offline Autopilot Profile " + profile.DisplayName;

            Version = 2049;

            ZtdCorrelationId = profile.Id;

            if (profile.ODataType.Equals("#microsoft.graph.activeDirectoryWindowsAutopilotDeploymentProfile"))
            {
                CloudAssignedDomainJoinMethod = 1;
            }
            else
            {
                CloudAssignedDomainJoinMethod = 0;
            }

            if (profile.DeviceNameTemplate.Length > 0)
            {
                CloudAssignedDeviceName = profile.DeviceNameTemplate;
            }

            CloudAssignedOobeConfig = 8 + 256;

            if (profile.OutOfBoxExperienceSettings.UserType.Equals("standard"))
            {
                CloudAssignedOobeConfig += 2;
            }

            if ((bool)profile.OutOfBoxExperienceSettings.HidePrivacySettings)
            {
                CloudAssignedOobeConfig += 4;
            }

            if ((bool)profile.OutOfBoxExperienceSettings.HideEULA)
            {
                CloudAssignedOobeConfig += 16;
            }

            if ((bool)profile.OutOfBoxExperienceSettings.SkipKeyboardSelectionPage)
            {
                CloudAssignedOobeConfig += 1024;
            }

            if (profile.OutOfBoxExperienceSettings.DeviceUsageType.Equals("shared"))
            {
                CloudAssignedOobeConfig += 32 + 64;
            }

            if (profile.Language.Length > 0)
            {
                CloudAssignedLanguage = profile.Language;
            }

            if ((bool)profile.OutOfBoxExperienceSettings.HideEscapeLink)
            {
                CloudAssignedForcedEnrollment = 1;
            }
            else
            {
                CloudAssignedForcedEnrollment = 0;
            }

            CloudAssignedTenantId = organization.Id;

            foreach (VerifiedDomain domain in organization.VerifiedDomains)
            {
                if ((bool)domain.IsDefault)
                {
                    CloudAssignedTenantDomain = domain.Name;
                }
            }

            int hideEscapeLink = 0;

            if (profile.OutOfBoxExperienceSettings.HideEscapeLink.HasValue)
            {
                hideEscapeLink = 1;
            }

            // Nest a ZeroTouchConfig within the CloudAssignedAadServerData object -> required for the JSON
            ZeroTouchConfig touchConfig = new ZeroTouchConfig(CloudAssignedTenantDomain, hideEscapeLink);
            CloudAssignedAadServerData zeroTouchConfig = new CloudAssignedAadServerData(touchConfig);

            // Serialize ZeroTouchConfig as JSON string
            this.CloudAssignedAadServerData = JsonConvert.SerializeObject(zeroTouchConfig,
                new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                }
                );
        }
    }
}