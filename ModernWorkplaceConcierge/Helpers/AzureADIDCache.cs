using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModernWorkplaceConcierge.Helpers
{
    /*
        Stores Azure AD Object IDs in order to avoid multiple queries for the same resource's display names

    */

    public class AzureADIDCache
    {
        // Store Object ID and displayName
        private Dictionary<String, String> graphCache;

        private string clientId;
        private IEnumerable<DirectoryRoleTemplate> roleTemplates;
        private IEnumerable<ServicePrincipal> servicePrincipals;
        private IEnumerable<NamedLocation> namedLocations;
        private GraphConditionalAccess graphConditionalAccess;

        public AzureADIDCache(string clientId = null)
        {
            this.clientId = clientId;
            this.graphCache = new Dictionary<string, string>();
            this.graphConditionalAccess = new GraphConditionalAccess(clientId);
        }

        public async System.Threading.Tasks.Task<List<string>> getUserDisplayNamesAsync(string[] userIDs)
        {
            List<String> displayNames = new List<string>();
            foreach (String userID in userIDs)
            {
                // Check for UID
                if (Guid.TryParse(userID, out Guid result))
                {
                    // Check if AAD Object in in cache
                    if (graphCache.ContainsKey(userID))
                    {
                        displayNames.Add(graphCache[userID]);
                    }
                    else
                    {
                        Microsoft.Graph.User user = await GraphHelper.GetUserById(userID, clientId);
                        graphCache.Add(user.Id, user.UserPrincipalName);
                        displayNames.Add(user.UserPrincipalName);
                    }
                }
                else
                {
                    displayNames.Add(userID);
                }
            }
            return displayNames;
        }

        public async System.Threading.Tasks.Task<List<string>> getGroupDisplayNamesAsync(string[] groupIDs)
        {
            List<String> displayNames = new List<string>();
            foreach (String groupID in groupIDs)
            {
                // Check for UID
                if (Guid.TryParse(groupID, out Guid result))
                {
                    // Check if AAD Object in in cache
                    if (graphCache.ContainsKey(groupID))
                    {
                        displayNames.Add(graphCache[groupID]);
                    }
                    else
                    {
                        Group group = await GraphHelper.GetGroup(groupID, clientId);
                        graphCache.Add(group.Id, group.DisplayName);
                        displayNames.Add(group.DisplayName);
                    }
                }
                else
                {
                    displayNames.Add(groupID);
                }
            }
            return displayNames;
        }

        public async System.Threading.Tasks.Task<List<string>> getRoleDisplayNamesAsync(string[] roleIDs)
        {
            if (roleTemplates == null || roleTemplates.Count() == 0)
            {
                roleTemplates = await GraphHelper.GetDirectoryRoleTemplates(clientId);
            }

            List<String> displayNames = new List<string>();
            foreach (String roleID in roleIDs)
            {
                // Check for UID
                if (Guid.TryParse(roleID, out Guid result))
                {
                    displayNames.Add(roleTemplates.Where(role => role.Id == roleID).Select(role => role.DisplayName).First());
                }
                else
                {
                    displayNames.Add(roleID);
                }
            }
            return displayNames;
        }

        public async Task<List<string>> getNamedLocationDisplayNamesAsync(string[] locationIDs)
        {
            if (namedLocations == null || namedLocations.Count() == 0)
            {
                namedLocations = await graphConditionalAccess.GetNamedLocationsAsync();
            }

            List<String> displayNames = new List<string>();
            foreach (String locationID in locationIDs)
            {
                // Check for UID
                if (Guid.TryParse(locationID, out Guid result))
                {
                    displayNames.Add(namedLocations.Where(loc => loc.Id == locationID).Select(loc => loc.DisplayName).First());
                }
                else
                {
                    displayNames.Add(locationID);
                }
            }
            return displayNames;
        }

        public async Task<List<string>> getApplicationDisplayNamesAsync(string[] applicationIDs)
        {
            if (servicePrincipals == null || !servicePrincipals.Any())
            {
                servicePrincipals = await GraphHelper.GetServicePrincipals(clientId);
            }

            List<String> displayNames = new List<string>();
            foreach (String applicationID in applicationIDs)
            {
                // Check for UID
                if (Guid.TryParse(applicationID, out Guid result))
                {
                    try
                    {
                        displayNames.Add(servicePrincipals.Where(app => app.AppId == applicationID).Select(app => app.AppDisplayName).First());
                    }
                    catch
                    {
                        if (AzureADApplicationIdentifiers.keyValuePairs.TryGetValue(applicationID, out string appDisplayName))
                        {
                            displayNames.Add(appDisplayName);
                        }
                        else
                        {
                            displayNames.Add(applicationID);
                        }
                    }
                }
                else
                {
                    displayNames.Add(applicationID);
                }
            }
            return displayNames;
        }
    }
}