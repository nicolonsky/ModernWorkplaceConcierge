[![Build Status](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_apis/build/status/nicolonsky.IntuneConcierge?branchName=master)](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_build/latest?definitionId=2&branchName=master)

# About

The [Modern Workplace Concierge](https://mwconcierge.azurewebsites.net/) is a helper tool to simplify your daily work with Microsoft 365 services. It allows you to:

* Import and export Intune configuration and settings
* Import and export Conditional Access policies
* Download OSD ready offline Autopilot profiles
* Download stored PowerShell scripts in Intune (as PowerShell)

The tool is built on ASP.NET and works with the Microsoft Graph Beta API (because on the Beta endpoint are more entities available).

### Supported Intune Configurations

The following Intune objects are included in exports:

* Compliance policies
* Configuration profiles
* PowerShell scripts
* Windows 10 update rings
* Enrollment restrictions
* Windows enrollment settings
* App protection policies
* App configuration policies
* Windows Autopilot deployment profiles
## Consent and Permissions

To Authenticate with the Microsoft Graph API a multi tenant Azure AD application performs authentication and you will need to provide admin consent to the Azure AD application before you can use this tool.

<img src="https://github.com/nicolonsky/ModernWorkplaceConcierge/blob/master/Doc/Consent.png" alt="Consent" width="25%">

As the tool performs only GET and POST requests to the Graph API no unitended or negative effects should occur. Additionally conditional access policies are imported as disabled to prevent a lockout.
