using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Graph;
using IntuneConcierge.Helpers;

namespace IntuneConcierge.Helpers
{
    public class EmbeddedWindowsAutopilotDeploymentProfile
    {
        public String CloudAssignedTenantDomain;
        String CloudAssignedTenantUpn;
        public int ForcedEnrollment;

        public EmbeddedWindowsAutopilotDeploymentProfile()
        {

        }
    }

    public class ZeroTouchWindowsAutopilotDeploymentProfile : EmbeddedWindowsAutopilotDeploymentProfile
    {

        EmbeddedWindowsAutopilotDeploymentProfile EmbeddedWindowsAutopilotDeploymentProfile;

        ZeroTouchWindowsAutopilotDeploymentProfile(EmbeddedWindowsAutopilotDeploymentProfile embedded)
        {
            this.EmbeddedWindowsAutopilotDeploymentProfile = embedded;
        }
    }

    public class WindowsAutopilotDeploymentProfile
    {
        String Comment_File;
        int Version;
        String ZtdCorrelationId;
        int CloudAssignedDomainJoinMethod;
        String CloudAssignedDeviceName;
        int CloudAssignedOobeConfig;
        String CloudAssignedLanguage;
        int CloudAssignedForcedEnrollment;
        String CloudAssignedTenantId;
        String CloudAssignedTenantDomain;
        int ForcedEnrollment;
        ZeroTouchWindowsAutopilotDeploymentProfile ZeroTouchConfig;


        public WindowsAutopilotDeploymentProfile (Microsoft.Graph.WindowsAutopilotDeploymentProfile profile)
        {
            this.Comment_File = "Profile " + profile.DisplayName;
            this.Version = 2049;
            this.ZtdCorrelationId = profile.Id;

            if (profile.ODataType == "#microsoft.graph.activeDirectoryWindowsAutopilotDeploymentProfile")
            {
                this.CloudAssignedDomainJoinMethod = 1;
            }
            else
            {
                this.CloudAssignedDomainJoinMethod = 0;
            }

            if (profile.DeviceNameTemplate.Length > 0)
            {
                this.CloudAssignedDeviceName = profile.DeviceNameTemplate;
            }

            this.CloudAssignedOobeConfig = 8;

            if  (profile.OutOfBoxExperienceSettings.UserType.Equals("standard"))
            {
                this.CloudAssignedOobeConfig += 2;
            }

            if ((bool)profile.OutOfBoxExperienceSettings.HidePrivacySettings)
            {
                this.CloudAssignedOobeConfig += 4;
            }

            if ((bool)profile.OutOfBoxExperienceSettings.HideEULA)
            {
                this.CloudAssignedOobeConfig += 16;
            }

            if ((bool)profile.OutOfBoxExperienceSettings.SkipKeyboardSelectionPage)
            {
                this.CloudAssignedOobeConfig += 1024;
            }

            if (profile.OutOfBoxExperienceSettings.DeviceUsageType.Equals("shared"))
            {
                this.CloudAssignedOobeConfig += 32 + 64;
            }

            if (profile.Language.Length > 0)
            {
                this.CloudAssignedLanguage = profile.Language;
            }

            if ((bool)profile.OutOfBoxExperienceSettings.HideEscapeLink)
            {
                this.CloudAssignedForcedEnrollment = 1;
            }
            else
            {
                this.CloudAssignedForcedEnrollment = 0;
            }


            var task = GraphHelper.GetOrgDetailsAsync();

            // get Org from Graph
            Organization organization = task.Result; ;

            this.CloudAssignedTenantId = organization.Id;

            foreach (VerifiedDomain domain in organization.VerifiedDomains)
            {
                if ((bool)domain.IsDefault)
                {
                    this.CloudAssignedTenantDomain = domain.Name;
                }

            }

            int hideEscapeLink = 0;

            if (profile.OutOfBoxExperienceSettings.HideEscapeLink.HasValue)
            {
                hideEscapeLink = 1;
            }

            this.ZeroTouchConfig.ForcedEnrollment =  hideEscapeLink; 
            this.ZeroTouchConfig.CloudAssignedTenantDomain = this.CloudAssignedTenantDomain;
        }
    }
}