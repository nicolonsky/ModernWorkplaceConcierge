[![Build Status](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_apis/build/status/nicolonsky.IntuneConcierge?branchName=master)](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_build/latest?definitionId=2&branchName=master)

# About

The [Modern Workplace Concierge](https://mwconcierge.azurewebsites.net/) is a helper tool to simplify your daily work with Microsoft 365 services. It allows you to:

* Import and export Intune configuration and settings
* Import and export Conditional Access policies
* Download OSD ready offline Autopilot profiles
* Re-Download stored PowerShell scripts in Intune (as PowerShell)
* Import trello boards to Microsoft Planner

The tool is built on ASP.NET and works with the Microsoft Graph Beta API (because on the Beta endpoint are more entities available).

## Supported Intune Configurations

The following Intune objects are included in exports:

* Compliance policies
* Configuration profiles
* PowerShell scripts
* Windows 10 update rings
* Enrollment restrictions
* Windows enrollment settings
* App protection policies
* App configuration policies
* Windows autopilot deployment profiles
* Scope tags
* RBAC Roles

## Consent and Permissions

To Authenticate with the Microsoft Graph API a multi tenant Azure AD application performs authentication and you will need to provide admin consent to the Azure AD application before you can use this tool.

<img src="https://github.com/nicolonsky/ModernWorkplaceConcierge/blob/master/Doc/Consent.png" alt="Consent" width="25%">

As the tool performs only GET and POST requests to the Graph API no unitended or negative effects should occur. Additionally conditional access policies are imported as disabled to prevent a lockout.

## Privacy

All up- and downloaded data is processed in memory and not stored persistent. No usage data with Azure AD tenant ID's or Azure AD user information is collected. The app uses Azure application insights and traces the performance markers [what-does-application-insights-monitor](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview#what-does-application-insights-monitor).

### Host your own instance

If you cannot use the public instance of the ModernWorkplaceConcierge (because of legal and/or compliance reasons) you can deploy an instance in your Azure tenant.
[Wiki documentation for Self-hosting a custom instance](https://github.com/nicolonsky/ModernWorkplaceConcierge/wiki/Self-hosting-a-custom-instance).

[![Deploy to Azure](https://azurecomcdn.azureedge.net/mediahandler/acomblog/media/Default/blog/deploybutton.png)](https://azuredeploy.net/?repository=https://github.com/nicolonsky/ModernWorkplaceConcierge/tree/master)

<a href="http://armviz.io/#/?load=https://raw.githubusercontent.com/nicolonsky/ModernWorkplaceConcierge/dev/azuredeploy.json" target="_blank">
  <img src="http://armviz.io/visualizebutton.png"/>
</a>
