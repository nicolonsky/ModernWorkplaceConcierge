using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace ModernWorkplaceConcierge.Helpers
{
    public class ConditionalAccessPolicies
    {
        [JsonProperty("@odata.context")]
        public String OdataContext { get; set; }
        [JsonProperty("value")]
        public ConditionalAccessPolicy[] Value { get; set; }

        public ConditionalAccessPolicies(String OdataContext, ConditionalAccessPolicy[] Value)
        {
            this.Value = Value;
            this.OdataContext = OdataContext;
        }
    }

    public class ConditionalAccessPolicy
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public object createdDateTime { get; set; }
        public object modifiedDateTime { get; set; }
        public string state { get; set; }
        public Grantcontrols grantControls { get; set; }
        public Conditions conditions { get; set; }
        public Sessioncontrols sessionControls { get; set; }
    }

    public class Grantcontrols
    {
        [JsonProperty("operator")]
        public string op { get; set; }
        public string[] builtInControls { get; set; }
        public object[] customAuthenticationFactors { get; set; }
        public object[] termsOfUse { get; set; }
    }

    public class Conditions
    {
        public object[] signInRiskLevels { get; set; }
        public object[] clientAppTypes { get; set; }
        public object platforms { get; set; }
        public object locations { get; set; }
        public DeviceStates deviceStates { get; set; }
        public Applications applications { get; set; }
        public Users users { get; set; }
    }

    public class DeviceStates
    {
        public string[] includeStates { get; set; }
        public string[] excludeStates { get; set; }
    }

    public class Applications
    {
        public string[] includeApplications { get; set; }
        public object[] excludeApplications { get; set; }
        public object[] includeUserActions { get; set; }
    }

    public class Users
    {
        public string[] includeUsers { get; set; }
        public object[] excludeUsers { get; set; }
        public object[] includeGroups { get; set; }
        public object[] excludeGroups { get; set; }
        public string[] includeRoles { get; set; }
        public object[] excludeRoles { get; set; }
    }

    public class Sessioncontrols
    {
        public object cloudAppSecurity { get; set; }
        public object signInFrequency { get; set; }
        public object persistentBrowser { get; set; }
        public Applicationenforcedrestrictions applicationEnforcedRestrictions { get; set; }
    }

    public class Applicationenforcedrestrictions
    {
        public bool isEnabled { get; set; }
    }

}