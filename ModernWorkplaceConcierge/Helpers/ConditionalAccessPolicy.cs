using Newtonsoft.Json;
using System;

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
        public object[] userRiskLevels { get; set; }
        public object[] clientAppTypes { get; set; }
        public Platforms platforms { get; set; }
        public Locations locations { get; set; }
        public Devices devices { get; set; }
        public Applications applications { get; set; }
        public Users users { get; set; }
    }

    public class Platforms
    {
        public string[] includePlatforms { get; set; }
        public string[] excludePlatforms { get; set; }
    }

    public class Locations
    {
        public string[] includeLocations { get; set; }
        public string[] excludeLocations { get; set; }
    }

    public class Devices
    {
        public string[] includeDeviceStates { get; set; }
        public string[] excludeDeviceStates { get; set; }
    }

    public class Applications
    {
        public string[] includeApplications { get; set; }
        public string[] excludeApplications { get; set; }
        public string[] includeUserActions { get; set; }
    }

    public class Users
    {
        public string[] includeUsers { get; set; }
        public string[] excludeUsers { get; set; }
        public string[] includeGroups { get; set; }
        public string[] excludeGroups { get; set; }
        public string[] includeRoles { get; set; }
        public string[] excludeRoles { get; set; }
    }

    public class Sessioncontrols
    {
        public object cloudAppSecurity { get; set; }
        public SignInFrequency signInFrequency { get; set; }
        public PersistentBrowser persistentBrowser { get; set; }
        public Applicationenforcedrestrictions applicationEnforcedRestrictions { get; set; }
    }

    public class Applicationenforcedrestrictions
    {
        public bool isEnabled { get; set; }
    }

    public class SignInFrequency
    {
        public int value { get; set; }
        public string type { get; set; }
        public bool isEnabled { get; set; }
    }

    public class PersistentBrowser
    {
        public string mode { get; set; }
        public bool isEnabled { get; set; }
    }
}