using ModernWorkplaceConcierge.Helpers;
using ModernWorkplaceConcierge.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    public class ConditionalAccessController : BaseController
    {
        private readonly string TEMPLATE_CA_POLICY_FOLDER_PATH = "~/Content/PolicySets/";

        [HttpPost]
        public async Task<ActionResult> Upload(HttpPostedFileBase[] files, OverwriteBehaviour overwriteBehaviour, string clientId)
        {
            SignalRMessage signalRMessage = new SignalRMessage(clientId);

            try
            {
                GraphConditionalAccess graphConditionalAccess = new GraphConditionalAccess(clientId);
                IEnumerable<ConditionalAccessPolicy> conditionalAccessPolicies = await graphConditionalAccess.GetConditionalAccessPoliciesAsync();
                List<string> uploadedConditionalAccessPolicies = new List<string>();

                if (files.Length > 0 && files[0].FileName.Contains(".json"))
                {
                    foreach (HttpPostedFileBase file in files)
                    {
                        BinaryReader binaryReader = new BinaryReader(file.InputStream);
                        byte[] binData = binaryReader.ReadBytes(file.ContentLength);
                        uploadedConditionalAccessPolicies.Add(Encoding.UTF8.GetString(binData));
                    }
                }
                else if (files.Length > 0 && files[0].FileName.Contains(".zip"))
                {
                    try
                    {
                        MemoryStream target = new MemoryStream();
                        files[0].InputStream.CopyTo(target);
                        byte[] data = target.ToArray();

                        using (var zippedStream = new MemoryStream(data))
                        {
                            using (var archive = new ZipArchive(zippedStream))
                            {
                                foreach (var entry in archive.Entries)
                                {
                                    try
                                    {
                                        if (entry != null)
                                        {
                                            using (var unzippedEntryStream = entry.Open())
                                            {
                                                using (var ms = new MemoryStream())
                                                {
                                                    unzippedEntryStream.CopyTo(ms);
                                                    var unzippedArray = ms.ToArray();
                                                    uploadedConditionalAccessPolicies.Add(Encoding.UTF8.GetString(unzippedArray));
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        signalRMessage.sendMessage("Error: " + e.Message);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        signalRMessage.sendMessage("Error: " + e.Message);
                    }
                }

                foreach (string uploadedPolicy in uploadedConditionalAccessPolicies)
                {
                    ConditionalAccessPolicy conditionalAccessPolicy = JsonConvert.DeserializeObject<ConditionalAccessPolicy>(uploadedPolicy);

                    switch (overwriteBehaviour)
                    {
                        case OverwriteBehaviour.DISCARD:
                            // Check for any policy with same name or id
                            if (conditionalAccessPolicies.All(p => !p.id.Contains(conditionalAccessPolicy.id) && conditionalAccessPolicies.All(policy => !policy.displayName.Equals(conditionalAccessPolicy.displayName))))
                            {
                                var response = await graphConditionalAccess.TryAddConditionalAccessPolicyAsync(conditionalAccessPolicy);
                            }
                            else
                            {
                                if (conditionalAccessPolicies.Any(p => p.id.Contains(conditionalAccessPolicy.id)))
                                {
                                    signalRMessage.sendMessage($"Discarding Policy '{conditionalAccessPolicy.displayName}' ({conditionalAccessPolicy.id}) already exists!");
                                }
                                else
                                {
                                    signalRMessage.sendMessage($"Discarding Policy '{conditionalAccessPolicy.displayName}' - policy with this name already exists!");
                                }
                            }
                            break;

                        case OverwriteBehaviour.IMPORT_AS_DUPLICATE:
                            await graphConditionalAccess.TryAddConditionalAccessPolicyAsync(conditionalAccessPolicy);
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_ID:

                            // match by object ID
                            if (conditionalAccessPolicies.Any(policy => policy.id.Equals(conditionalAccessPolicy.id)))
                            {
                                await graphConditionalAccess.PatchConditionalAccessPolicyAsync(conditionalAccessPolicy);
                            }
                            // Create a new policy
                            else
                            {
                                var result = await graphConditionalAccess.TryAddConditionalAccessPolicyAsync(conditionalAccessPolicy);
                            }
                            break;

                        case OverwriteBehaviour.OVERWRITE_BY_NAME:
                            if (conditionalAccessPolicies.Any(policy => policy.displayName.Equals(conditionalAccessPolicy.displayName)))
                            {
                                string replaceObjectId = conditionalAccessPolicies.Where(policy => policy.displayName.Equals(conditionalAccessPolicy.displayName)).Select(policy => policy.id).First();
                                conditionalAccessPolicy.id = replaceObjectId;
                                await graphConditionalAccess.PatchConditionalAccessPolicyAsync(conditionalAccessPolicy);
                            }
                            else
                            {
                                var result = await graphConditionalAccess.TryAddConditionalAccessPolicyAsync(conditionalAccessPolicy);
                            }

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                signalRMessage.sendMessage("Error: " + e.Message);
            }

            signalRMessage.sendMessage("Done#!");
            return new HttpStatusCodeResult(204);
        }

        [HttpPost]
        public async Task<ActionResult> DeployBaseline(string displayName, string selectedBaseline, string policyPrefix, string allowLegacyAuth, string clientId = null)
        {
            SignalRMessage signalR = new SignalRMessage(clientId);
            GraphConditionalAccess graphConditionalAccess = new GraphConditionalAccess(clientId);

            signalR.sendMessage("Selected baseline: " + selectedBaseline);

            try
            {
                // Create exclusion group (if the group already exists we retrieve the ID)
                Microsoft.Graph.Group createdGroup = await GraphHelper.CreateGroup(displayName, clientId);
                List<string> groupsCreated = new List<string>
                {
                    createdGroup.Id
                };

                // Load CA policies for policy set
                string[] filePaths = Directory.GetFiles(Server.MapPath(TEMPLATE_CA_POLICY_FOLDER_PATH + selectedBaseline), "*.json");

                if (filePaths.Length == 0)
                {
                    signalR.sendMessage($"Warning no Conditional Access Policies found within selected set ({selectedBaseline})!");
                }

                // Modify exclusions & Display Name
                List<ConditionalAccessPolicy> conditionalAccessPolicies = new List<ConditionalAccessPolicy>();

                foreach (string filePath in filePaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            using (var streamReader = new StreamReader(filePath, Encoding.UTF8))
                            {
                                string textCaPolicy = streamReader.ReadToEnd();

                                // Modify properties on template
                                ConditionalAccessPolicy conditionalAccessPolicy = JsonConvert.DeserializeObject<ConditionalAccessPolicy>(textCaPolicy);
                                conditionalAccessPolicy.conditions.users.excludeGroups = groupsCreated.ToArray();
                                string placeholder = "<RING> -";
                                int startDisplayName = conditionalAccessPolicy.displayName.IndexOf(placeholder) + placeholder.Length;
                                conditionalAccessPolicy.displayName = conditionalAccessPolicy.displayName.Substring(startDisplayName, conditionalAccessPolicy.displayName.Length - startDisplayName);
                                conditionalAccessPolicy.displayName = conditionalAccessPolicy.displayName.Insert(0, policyPrefix).Replace("<PREFIX> -", "").Trim();

                                // Check for legacy auth exclusion group
                                if (conditionalAccessPolicy.conditions.clientAppTypes.Contains("other") && conditionalAccessPolicy.grantControls.builtInControls.Contains("block"))
                                {
                                    // Wee need to initialize a new list to avoid modifications to the existing!
                                    List<String> newGroupsCreated = new List<String>(groupsCreated);
                                    Microsoft.Graph.Group allowLegacyAuthGroup = await GraphHelper.CreateGroup(allowLegacyAuth, clientId);
                                    newGroupsCreated.Add(allowLegacyAuthGroup.Id);
                                    conditionalAccessPolicy.conditions.users.excludeGroups = newGroupsCreated.ToArray();
                                }

                                // Create the policy
                                await graphConditionalAccess.TryAddConditionalAccessPolicyAsync(conditionalAccessPolicy);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        signalR.sendMessage("Error: " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                signalR.sendMessage("Error: " + e.Message);
            }

            signalR.sendMessage("Done#!");
            return new HttpStatusCodeResult(204);
        }

        public ViewResult DeployBaseline()
        {
            List<String> dirs = new List<string>();

            String[] XmlFiles = Directory.GetDirectories(Server.MapPath(TEMPLATE_CA_POLICY_FOLDER_PATH));

            for (int x = 0; x < XmlFiles.Length; x++)
                dirs.Add(Path.GetFileNameWithoutExtension(XmlFiles[x]));

            return View(dirs);
        }

        public ViewResult Import()
        {
            return View();
        }

        // GET: ConditionalAccess
        public ViewResult Index()
        {
            return View();
        }

        public async Task<ActionResult> DownloadAll(string clientId = null)
        {
            SignalRMessage signalR = new SignalRMessage(clientId);

            try
            {
                GraphConditionalAccess graphConditionalAccess = new GraphConditionalAccess(clientId);
                IEnumerable<ConditionalAccessPolicy> conditionalAccessPolicies = await graphConditionalAccess.GetConditionalAccessPoliciesAsync();

                using (MemoryStream ms = new MemoryStream())
                {
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        foreach (ConditionalAccessPolicy item in conditionalAccessPolicies)
                        {
                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                            string displayName = item.displayName;

                            char[] illegal = Path.GetInvalidFileNameChars();

                            foreach (char illegalChar in illegal)
                            {
                                displayName = displayName.Replace(illegalChar, '-');
                            }

                            var zipArchiveEntry = archive.CreateEntry(displayName + "_" + item.id.Substring(0,8) + ".json", CompressionLevel.Fastest);

                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }
                    }

                    string domainName = await GraphHelper.GetDefaultDomain(clientId);
                    return File(ms.ToArray(), "application/zip", "ConditionalAccessConfig_" + domainName + ".zip");
                }
            }
            catch (Exception e)
            {
                signalR.sendMessage("Error " + e);
            }
            return new HttpStatusCodeResult(204);
        }

        public ViewResult Documentation()
        {
            return View();
        }

        [CustomAuthorization(Roles = ("AdvancedUser"))]
        public async Task<ActionResult> ClearAll(bool confirm = false)
        {
            try
            {
                GraphConditionalAccess graphConditionalAccess = new GraphConditionalAccess(null);
                if (confirm)
                {
                    await graphConditionalAccess.ClearConditonalAccessPolicies();
                }
                return new HttpStatusCodeResult(204);
            }
            catch (Exception e)
            {
                Flash(e.Message);
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<ActionResult> CreateDocumentation(string clientId = null)
        {
            IEnumerable<ConditionalAccessPolicy> conditionalAccessPolicies = null;
            SignalRMessage signalR = new SignalRMessage(clientId);
            
            try
            {
                GraphConditionalAccess graphConditionalAccess = new GraphConditionalAccess(clientId);
                conditionalAccessPolicies = await graphConditionalAccess.GetConditionalAccessPoliciesAsync();

                DataTable dataTable = new DataTable();

                dataTable.BeginInit();
                dataTable.Columns.Add("Name");
                dataTable.Columns.Add("State");

                // Assignments: first include then exclude
                dataTable.Columns.Add("IncludeUsers");
                dataTable.Columns.Add("IncludeGroups");
                dataTable.Columns.Add("IncludeRoles");

                dataTable.Columns.Add("ExcludeUsers");
                dataTable.Columns.Add("ExcludeGroups");
                dataTable.Columns.Add("ExcludeRoles");

                dataTable.Columns.Add("IncludeApps");
                dataTable.Columns.Add("ExcludeApps");
                dataTable.Columns.Add("IncludeUserActions");
                dataTable.Columns.Add("ClientAppTypes");
                dataTable.Columns.Add("IncludePlatforms");
                dataTable.Columns.Add("ExcludePlatforms");
                dataTable.Columns.Add("IncludeLocations");
                dataTable.Columns.Add("ExcludeLocations");
                dataTable.Columns.Add("IncludeDeviceStates");
                dataTable.Columns.Add("ExcludeDeviceStates");
                dataTable.Columns.Add("GrantControls");
                dataTable.Columns.Add("GrantControlsOperator");
                dataTable.Columns.Add("ApplicationEnforcedRestrictions");
                dataTable.Columns.Add("CloudAppSecurity");
                dataTable.Columns.Add("PersistentBrowser");
                dataTable.Columns.Add("SignInFrequency");

                // Init cache for AAD Object ID's in CA policies
                AzureADIDCache azureADIDCache = new AzureADIDCache(clientId);

                foreach (ConditionalAccessPolicy conditionalAccessPolicy in conditionalAccessPolicies)
                {
                    try
                    {
                        // Init a new row
                        DataRow row = dataTable.NewRow();

                        row["Name"] = conditionalAccessPolicy.displayName;
                        row["State"] = conditionalAccessPolicy.state;

                        row["IncludeUsers"] = $"\"{String.Join("\n", await azureADIDCache.getUserDisplayNamesAsync(conditionalAccessPolicy.conditions.users.includeUsers))}\"";
                        row["ExcludeUsers"] = $"\"{String.Join("\n", await azureADIDCache.getUserDisplayNamesAsync(conditionalAccessPolicy.conditions.users.excludeUsers))}\"";
                        row["IncludeGroups"] = $"\"{String.Join("\n", await azureADIDCache.getGroupDisplayNamesAsync(conditionalAccessPolicy.conditions.users.includeGroups))}\"";
                        row["ExcludeGroups"] = $"\"{String.Join("\n", await azureADIDCache.getGroupDisplayNamesAsync(conditionalAccessPolicy.conditions.users.excludeGroups))}\"";
                        row["IncludeRoles"] = $"\"{String.Join("\n", await azureADIDCache.getRoleDisplayNamesAsync(conditionalAccessPolicy.conditions.users.includeRoles))}\"";
                        row["ExcludeRoles"] = $"\"{String.Join("\n", await azureADIDCache.getRoleDisplayNamesAsync(conditionalAccessPolicy.conditions.users.excludeRoles))}\"";

                        row["IncludeApps"] = $"\"{String.Join("\n", await azureADIDCache.getApplicationDisplayNamesAsync(conditionalAccessPolicy.conditions.applications.includeApplications))}\"";
                        row["ExcludeApps"] = $"\"{String.Join("\n", await azureADIDCache.getApplicationDisplayNamesAsync(conditionalAccessPolicy.conditions.applications.excludeApplications))}\"";

                        row["IncludeUserActions"] = $"\"{String.Join("\n", await azureADIDCache.getApplicationDisplayNamesAsync(conditionalAccessPolicy.conditions.applications.includeUserActions))}\"";

                        if (conditionalAccessPolicy.conditions.platforms != null && conditionalAccessPolicy.conditions.platforms.includePlatforms != null)
                        {
                            row["IncludePlatforms"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.platforms.includePlatforms)}\"";
                        }

                        if (conditionalAccessPolicy.conditions.platforms != null && conditionalAccessPolicy.conditions.platforms.excludePlatforms != null)
                        {
                            row["ExcludePlatforms"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.platforms.excludePlatforms)}\"";
                        }

                        if (conditionalAccessPolicy.conditions.locations != null && conditionalAccessPolicy.conditions.locations.includeLocations != null)
                        {
                            row["IncludeLocations"] = $"\"{String.Join("\n", await azureADIDCache.getNamedLocationDisplayNamesAsync(conditionalAccessPolicy.conditions.locations.includeLocations))}\"";
                        }

                        if (conditionalAccessPolicy.conditions.locations != null && conditionalAccessPolicy.conditions.locations.excludeLocations != null)
                        {
                            row["ExcludeLocations"] = $"\"{String.Join("\n", await azureADIDCache.getNamedLocationDisplayNamesAsync(conditionalAccessPolicy.conditions.locations.excludeLocations))}\"";
                        }

                        row["ClientAppTypes"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.clientAppTypes)}\"";

                        if (conditionalAccessPolicy.conditions.devices != null && conditionalAccessPolicy.conditions.devices.includeDeviceStates != null)
                        {
                            row["IncludeDeviceStates"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.devices.includeDeviceStates)}\"";
                        }

                        if (conditionalAccessPolicy.conditions.devices != null && conditionalAccessPolicy.conditions.devices.excludeDeviceStates != null)
                        {
                            row["IncludeDeviceStates"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.devices.excludeDeviceStates)}\"";
                        }

                        if (conditionalAccessPolicy.grantControls != null && conditionalAccessPolicy.grantControls.builtInControls != null)
                        {
                            row["GrantControls"] = $"\"{String.Join("\n", conditionalAccessPolicy.grantControls.builtInControls)}\"";
                            row["GrantControlsOperator"] = $"\"{String.Join("\n", conditionalAccessPolicy.grantControls.op)}\"";
                        }

                        if (conditionalAccessPolicy.sessionControls != null && conditionalAccessPolicy.sessionControls.applicationEnforcedRestrictions != null)
                        {
                            row["ApplicationEnforcedRestrictions"] = $"\"{String.Join("\n", conditionalAccessPolicy.sessionControls.applicationEnforcedRestrictions.isEnabled)}\"";
                        }

                        if (conditionalAccessPolicy.sessionControls != null && conditionalAccessPolicy.sessionControls.cloudAppSecurity != null)
                        {
                            row["CloudAppSecurity"] = $"\"{String.Join("\n", conditionalAccessPolicy.sessionControls.cloudAppSecurity)}\"";
                        }

                        if (conditionalAccessPolicy.sessionControls != null && conditionalAccessPolicy.sessionControls.persistentBrowser != null)
                        {
                            row["PersistentBrowser"] = conditionalAccessPolicy.sessionControls.persistentBrowser.mode;
                        }

                        if (conditionalAccessPolicy.sessionControls != null && conditionalAccessPolicy.sessionControls.signInFrequency != null)
                        {
                            row["SignInFrequency"] = conditionalAccessPolicy.sessionControls.signInFrequency.value + " " + conditionalAccessPolicy.sessionControls.signInFrequency.type;
                        }

                        // Add new row to table
                        dataTable.Rows.Add(row);
                    }
                    catch (Exception e)
                    {
                        signalR.sendMessage("Error: " + e);
                    }
                }

                // Convert datatable to CSV string output

                StringBuilder sb = new StringBuilder();
                IEnumerable<string> columnNames = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
                sb.AppendLine(string.Join(",", columnNames));

                foreach (DataRow row in dataTable.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    sb.AppendLine(string.Join(",", fields));
                }

                string domainName = await GraphHelper.GetDefaultDomain(clientId);

                if (!string.IsNullOrEmpty(clientId))
                {
                    signalR.sendMessage("Success: Report generated");
                }

                return File(Encoding.ASCII.GetBytes(sb.ToString()), "text/csvt", "ConditionalAccessReport_" + domainName + ".csv");

            }
            catch (Exception e)
            {
                signalR.sendMessage($"Error {e.Message}");
            }

            return new HttpStatusCodeResult(204);
        }
    }
}