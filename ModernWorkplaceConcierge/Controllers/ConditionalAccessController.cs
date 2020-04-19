using ModernWorkplaceConcierge.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Data;

namespace ModernWorkplaceConcierge.Controllers
{

    [Authorize]
    public class ConditionalAccessController : BaseController
    {
        public readonly string TEMPLATE_CA_POLICY_FOLDER_PATH = "~/Content/PolicySets/";

        [HttpPost]
        public async Task<ActionResult> Upload(HttpPostedFileBase[] files, string clientId)
        {
            SignalRMessage signalR = new SignalRMessage();
            signalR.clientId = clientId;
            try
            {
                if (files.Length > 0 && files[0].FileName.Contains(".json"))
                {
                    foreach (HttpPostedFileBase file in files)
                    {

                        try
                        {
                            BinaryReader b = new BinaryReader(file.InputStream);
                            byte[] binData = b.ReadBytes(file.ContentLength);
                            string result = Encoding.UTF8.GetString(binData);

                            var success = await GraphHelper.ImportCaConfig(result, clientId);
                        }
                        catch (Exception e)
                        {
                            signalR.sendMessage("Error: " + e.Message);
                        }
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
                                                    string result = Encoding.UTF8.GetString(unzippedArray);

                                                    var success = await GraphHelper.ImportCaConfig(result, clientId);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        signalR.sendMessage("Error: " + e.Message);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        signalR.sendMessage("Error: " + e.Message);
                    }
                }
                else if (files.Length > 0)
                {
                    signalR.sendMessage("Error: unsupported file type" + files[0].FileName);
                }
            }
            catch (Exception e)
            {
                signalR.sendMessage("Error: " + e.Message);
            }

            signalR.sendMessage("Done#!");
            return new HttpStatusCodeResult(204);
        }

        [HttpPost]
        public async Task<ActionResult> DeployBaseline(string displayName, string selectedBaseline, string policyPrefix, string allowLegacyAuth, string clientId = null)
        {
            SignalRMessage signalR = new SignalRMessage
            {
                clientId = clientId
            };

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
                string[] filePaths = Directory.GetFiles(Server.MapPath(TEMPLATE_CA_POLICY_FOLDER_PATH + selectedBaseline),"*.json");

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
                                await GraphHelper.ImportCaConfig(JsonConvert.SerializeObject(conditionalAccessPolicy), clientId);
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
            SignalRMessage signalR = new SignalRMessage
            {
                clientId = clientId
            };
            try {
                string ca = await GraphHelper.GetConditionalAccessPoliciesAsync(clientId);

                ConditionalAccessPolicies conditionalAccessPolicies = JsonConvert.DeserializeObject<ConditionalAccessPolicies>(ca);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        foreach (ConditionalAccessPolicy item in conditionalAccessPolicies.Value)
                        {
                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                            string displayName = item.displayName;

                            char[] illegal = Path.GetInvalidFileNameChars();
                            
                            foreach (char illegalChar in illegal)
                            {
                                displayName = displayName.Replace(illegalChar, '-');
                            }

                            var zipArchiveEntry = archive.CreateEntry(displayName + "_" + item.id + ".json", CompressionLevel.Fastest);

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

        public async Task<ActionResult> ClearAll(bool confirm = false)
        {
            if (confirm)
            {
                await GraphHelper.ClearConditonalAccessPolicies();
            }
            return new HttpStatusCodeResult(204);
        }

        public async Task<FileResult> CreateDocumentation(string clientId = null)
        {

            SignalRMessage signalR = new SignalRMessage
            {
                clientId = clientId
            };

            if (!string.IsNullOrEmpty(clientId))
            {
                signalR.sendMessage("Building report....");
            }

            string ca = await GraphHelper.GetConditionalAccessPoliciesAsync(clientId);

            ConditionalAccessPolicies conditionalAccessPolicies = JsonConvert.DeserializeObject<ConditionalAccessPolicies>(ca);

            DataTable dataTable = new DataTable();

            dataTable.BeginInit();
            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("State");
            dataTable.Columns.Add("IncludedUsers");
            dataTable.Columns.Add("ExcludedUsers");
            dataTable.Columns.Add("IncludedGroups");
            dataTable.Columns.Add("ExcludedGroups");
            dataTable.Columns.Add("IncludedRoles");
            dataTable.Columns.Add("ExcludedRoles");
            dataTable.Columns.Add("IncludedApps");
            dataTable.Columns.Add("ExcludedApps");
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

            foreach (ConditionalAccessPolicy conditionalAccessPolicy in conditionalAccessPolicies.Value)
            {
                try
                {
                    // Init a new row
                    DataRow row = dataTable.NewRow();

                    row["Name"] = conditionalAccessPolicy.displayName;
                    row["State"] = conditionalAccessPolicy.state;
                    row["IncludedUsers"] = $"\"{String.Join("\n", await azureADIDCache.getUserDisplayNamesAsync(conditionalAccessPolicy.conditions.users.includeUsers))}\"";
                    row["ExcludedUsers"] = $"\"{String.Join("\n", await azureADIDCache.getUserDisplayNamesAsync(conditionalAccessPolicy.conditions.users.excludeUsers))}\"";
                    row["IncludedGroups"] = $"\"{String.Join("\n", await azureADIDCache.getGroupDisplayNamesAsync(conditionalAccessPolicy.conditions.users.includeGroups))}\"";
                    row["ExcludedGroups"] = $"\"{String.Join("\n", await azureADIDCache.getGroupDisplayNamesAsync(conditionalAccessPolicy.conditions.users.excludeGroups))}\"";
                    row["IncludedRoles"] = $"\"{String.Join("\n", await azureADIDCache.getRoleDisplayNamesAsync(conditionalAccessPolicy.conditions.users.includeRoles))}\"";
                    row["ExcludedRoles"] = $"\"{String.Join("\n", await azureADIDCache.getRoleDisplayNamesAsync(conditionalAccessPolicy.conditions.users.excludeRoles))}\"";
                    row["IncludedApps"] = $"\"{String.Join("\n", azureADIDCache.getApplicationDisplayNames(conditionalAccessPolicy.conditions.applications.includeApplications))}\"";
                    row["ExcludedApps"] = $"\"{String.Join("\n", azureADIDCache.getApplicationDisplayNames(conditionalAccessPolicy.conditions.applications.excludeApplications))}\"";
                    

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
                        row["IncludeLocations"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.locations.includeLocations)}\"";
                    }

                    if (conditionalAccessPolicy.conditions.locations != null && conditionalAccessPolicy.conditions.locations.excludeLocations != null)
                    {
                        row["ExcludeLocations"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.locations.excludeLocations)}\"";
                    }

                    row["ClientAppTypes"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.clientAppTypes)}\"";

                    if (conditionalAccessPolicy.conditions.deviceStates != null && conditionalAccessPolicy.conditions.deviceStates.includeStates != null)
                    {
                        row["IncludeDeviceStates"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.deviceStates.includeStates)}\"";
                    }

                    if (conditionalAccessPolicy.conditions.deviceStates != null && conditionalAccessPolicy.conditions.deviceStates.excludeStates != null)
                    {
                        row["IncludeDeviceStates"] = $"\"{String.Join("\n", conditionalAccessPolicy.conditions.deviceStates.excludeStates)}\"";
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
                        row["PersistentBrowser"] = $"\"{String.Join("\n", conditionalAccessPolicy.sessionControls.persistentBrowser.mode)}\"";
                    }

                    if (conditionalAccessPolicy.sessionControls != null && conditionalAccessPolicy.sessionControls.signInFrequency != null)
                    {
                        row["SignInFrequency"] = $"\"{String.Join("\n", conditionalAccessPolicy.sessionControls.signInFrequency.isEnabled)}\"";
                        row["SignInFrequency"] = $"\"{String.Join("\n", conditionalAccessPolicy.sessionControls.signInFrequency.type)}\"";
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

            if (!string.IsNullOrEmpty(clientId))
            {
                signalR.sendMessage("Success: Report generated");
            }

            string domainName = await GraphHelper.GetDefaultDomain(clientId);

            return File(Encoding.ASCII.GetBytes(sb.ToString()), "text/csvt", "ConditionalAccessReport_" + domainName +".csv");
        }
    }   
}