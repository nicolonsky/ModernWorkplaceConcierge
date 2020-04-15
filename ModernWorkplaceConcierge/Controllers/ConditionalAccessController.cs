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
        public async Task<ActionResult> CreateGroup(string displayName, string selectedBaseline, string policyPrefix, string allowLegacyAuth, string clientId = null)
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

        public async Task<FileResult> DownloadAll(string clientId = null)
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

                            var zipArchiveEntry = archive.CreateEntry(item.displayName + "_" + item.id + ".json", CompressionLevel.Fastest);

                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }
                    }

                    string domainName = await GraphHelper.GetDefaultDomain();

                    return File(ms.ToArray(), "application/zip", "ConditionalAccessConfig_" + domainName + ".zip");
                }
            }
            catch (Exception e)
            {
                signalR.sendMessage("Error: " + e.Message);
            }

            return null;
        }

        public async Task<FileResult> CreateDocumentation(string clientId = null)
        {
            SignalRMessage signalR = new SignalRMessage
            {
                clientId = clientId
            };
            try
            {
                string ca = await GraphHelper.GetConditionalAccessPoliciesAsync(clientId);

                ConditionalAccessPolicies conditionalAccessPolicies = JsonConvert.DeserializeObject<ConditionalAccessPolicies>(ca);

                DataTable dataTable = new DataTable();

                dataTable.BeginInit();
                dataTable.Columns.Add("Name");
                dataTable.Columns.Add("Description");
                dataTable.Columns.Add("IncludedUsers");
                dataTable.Columns.Add("ExcludedUsers");
                dataTable.Columns.Add("IncludedGroups");
                dataTable.Columns.Add("ExcludedGroups");
                dataTable.Columns.Add("IncludedRoles");
                dataTable.Columns.Add("ExcludedRoles");
                dataTable.Columns.Add("IncludedApps");
                dataTable.Columns.Add("ExcludedApps");

                foreach (ConditionalAccessPolicy conditionalAccessPolicy in conditionalAccessPolicies.Value)
                {
                    DataRow row = dataTable.NewRow();
                    row["Name"] = conditionalAccessPolicy.displayName;
                    row["IncludedUsers"] = String.Join(";", conditionalAccessPolicy.conditions.users.includeUsers);
                    row["ExcludedUsers"] = String.Join(";", conditionalAccessPolicy.conditions.users.excludeUsers);
                    row["IncludedGroups"] = String.Join(";", conditionalAccessPolicy.conditions.users.includeGroups);
                    row["ExcludedGroups"] = String.Join(";", conditionalAccessPolicy.conditions.users.excludeGroups);
                    row["IncludedRoles"] = String.Join(";", conditionalAccessPolicy.conditions.users.includeRoles);
                    row["ExcludedRoles"] = String.Join(";", conditionalAccessPolicy.conditions.users.excludeRoles);

                    dataTable.Rows.Add(row);

                }

                StringBuilder sb = new StringBuilder();

                IEnumerable<string> columnNames = dataTable.Columns.Cast<DataColumn>().
                                                  Select(column => column.ColumnName);
                sb.AppendLine(string.Join(",", columnNames));

                foreach (DataRow row in dataTable.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    sb.AppendLine(string.Join(",", fields));
                }


                return File(Encoding.ASCII.GetBytes(sb.ToString()), "application/text", "ConditionalAccessReport.csv");

            }
            catch (Exception e)
            {
                signalR.sendMessage("Error: " + e.Message);
            }

            return null;
        }
    }   
}