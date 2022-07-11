[![Build Status](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_apis/build/status/nicolonsky.IntuneConcierge?branchName=master)](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_build/latest?definitionId=2&branchName=master)

| :exclamation:  Thank you for using the Modern Workplace Concierge and your support. This project is archived and not actively being maintained. Feel free to fork the project or deploy it to your own Azure tenant   |
|-----------------------------------------|

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

## Privacy

The app uses Azure application insights and traces performance markers [what-does-application-insights-monitor](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview#what-does-application-insights-monitor). All data is processed in memory and not stored persistent or used for further processing.

### Host your own instance

If you are not allowed to or don't like to use the public instance of the Modern Workplace Concierge you can deploy an instance in your Azure tenant & setup an app registration.
[Wiki documentation for Self-hosting a custom instance](https://github.com/nicolonsky/ModernWorkplaceConcierge/wiki/Self-hosting-a-custom-instance).

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fnicolonsky%2FModernWorkplaceConcierge%2Fmaster%2Fazuredeploy.json)

<a href="http://armviz.io/#/?load=https://raw.githubusercontent.com/nicolonsky/ModernWorkplaceConcierge/master/azuredeploy.json" target="_blank">
  <img src="https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.svg?sanitize=true"/>
</a>
