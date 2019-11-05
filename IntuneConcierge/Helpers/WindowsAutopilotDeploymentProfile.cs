using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Graph;
using IntuneConcierge.Helpers;
using ServiceStack;

namespace IntuneConcierge.Helpers
{
    public class EmbeddedWindowsAutopilotDeploymentProfile
    {
        public String CloudAssignedTenantDomain;
        public String CloudAssignedTenantUpn;
        public int ForcedEnrollment;

        public EmbeddedWindowsAutopilotDeploymentProfile()
        {

        }
    }

    public class ZeroTouchWindowsAutopilotDeploymentProfile : EmbeddedWindowsAutopilotDeploymentProfile
    {

        public EmbeddedWindowsAutopilotDeploymentProfile EmbeddedWindowsAutopilotDeploymentProfile;

        public ZeroTouchWindowsAutopilotDeploymentProfile(EmbeddedWindowsAutopilotDeploymentProfile embedded)
        {
            EmbeddedWindowsAutopilotDeploymentProfile = embedded;
        }
    }

    public class WindowsAutopilotDeploymentProfile
    {

        public String Comment_File;
        public int Version;
        public String ZtdCorrelationId;
        public int CloudAssignedDomainJoinMethod;
        public String CloudAssignedDeviceName;
        public int CloudAssignedOobeConfig;
        public String CloudAssignedLanguage;
        public int CloudAssignedForcedEnrollment;
        public String CloudAssignedTenantId;
        public String CloudAssignedTenantDomain;
        public ZeroTouchWindowsAutopilotDeploymentProfile ZeroTouchConfig;

        public WindowsAutopilotDeploymentProfile (Microsoft.Graph.WindowsAutopilotDeploymentProfile profile, Microsoft.Graph.Organization organization)
        {
            Comment_File = "Profile " + profile.DisplayName;
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

            CloudAssignedOobeConfig = 8;

            if  (profile.OutOfBoxExperienceSettings.UserType.Equals("standard"))
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

            ZeroTouchConfig.ForcedEnrollment =  hideEscapeLink; 
            ZeroTouchConfig.CloudAssignedTenantDomain = this.CloudAssignedTenantDomain;
        }
    }
}