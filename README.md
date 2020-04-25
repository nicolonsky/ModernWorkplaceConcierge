[![Build Status](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_apis/build/status/nicolonsky.IntuneConcierge?branchName=master)](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_build/latest?definitionId=2&branchName=master)

# About

The [Modern Workplace Concierge](https://mwconcierge.azurewebsites.net/) is a helper tool to simplify your daily work with Microsoft 365 services. It allows you to:

* Import and export Intune configuration and settings
* Import and export Conditional Access policies
* Document Conditional Access policies
* Deploy a Conditional Access baseline
* Download OSD ready offline Autopilot profiles
* Re-download uploaded PowerShell scripts from Intune
* Import trello boards to Microsoft Planner

The tool is built on ASP.NET and works with the Microsoft Graph Beta API.

## Supported entities

Supported configuration in imports and exports are documented on this project's [wiki](https://github.com/nicolonsky/ModernWorkplaceConcierge/wiki/Entities-supported-in-exports-and-imports).

## Consent and Permissions

To Authenticate with the Microsoft Graph API a multi tenant Azure AD application performs authentication and you will need to provide admin consent to the Azure AD application before you can use this tool.

<img src="https://github.com/nicolonsky/ModernWorkplaceConcierge/blob/master/Doc/Consent.png" alt="Consent" width="25%">

## Privacy

The app uses Azure application insights and traces performance markers [what-does-application-insights-monitor](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview#what-does-application-insights-monitor). Data of your tenant is processed in memory and not stored persistent or further processed.

### Host your own instance

If you cannot use the public instance of the ModernWorkplaceConcierge (because of legal and/or compliance reasons) you can deploy an instance in your Azure tenant.
[Wiki documentation for Self-hosting a custom instance](https://github.com/nicolonsky/ModernWorkplaceConcierge/wiki/Self-hosting-a-custom-instance).

[![Deploy to Azure](https://azurecomcdn.azureedge.net/mediahandler/acomblog/media/Default/blog/deploybutton.png)](https://azuredeploy.net/?repository=https://github.com/nicolonsky/ModernWorkplaceConcierge/tree/master)

<a href="http://armviz.io/#/?load=https://raw.githubusercontent.com/nicolonsky/ModernWorkplaceConcierge/master/azuredeploy.json" target="_blank">
  <img src="http://armviz.io/visualizebutton.png"/>
</a>
