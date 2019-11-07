[![Build Status](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_apis/build/status/nicolonsky.IntuneConcierge?branchName=master)](https://dev.azure.com/nicolonsky/ModernWorkplaceTools/_build/latest?definitionId=2&branchName=master)

## About

The [Intune Concierge](https://intuneconcierge.azurewebsites.net) is a helper tool to simplify your daily work with Microsoft Intune. It allows you to:

* export (backup) various of your Intune configurations like device configurations and other configurations
* download Autopilot offline Profiles to do Autopilot deployments with a AutopilotConfiguration.json 
* [comming soon] import (restore) your exported Intune configurations (also helpful when managing multiple tenants)

The tool is built on ASP.NET and works with the Microsoft Graph Beta API (because on the Beta endpoint are more Intune objects available).
To Authenticate with the Microsoft Graph API a multi tenant Azure AD application performs authentication and you will need to consent to the Azure AD application before you can use this tool.

