using ModernWorkplaceConcierge.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    public class ConditionalAccessController : BaseController
    {
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


            // Create exclusion group

            Microsoft.Graph.Group createdGroup = await GraphHelper.CreateGroup(displayName, clientId);
            List<String> groupsCreated = new List<string>();
            groupsCreated.Add(createdGroup.Id);

            // Load CA policies

            string[] filePaths = Directory.GetFiles(Server.MapPath("~/Content/PolicySets/" + selectedBaseline));

            // Modify exclusions & Display Name

            List<ConditionalAccessPolicy> conditionalAccessPolicies = new List<ConditionalAccessPolicy>();

            foreach(String filePath in filePaths)
            {
                using (var streamReader = new StreamReader(filePath, Encoding.UTF8))
                {
                    string textCaPolicy = streamReader.ReadToEnd();

                    ConditionalAccessPolicy conditionalAccessPolicy =  JsonConvert.DeserializeObject<ConditionalAccessPolicy>(textCaPolicy);
                    conditionalAccessPolicy.conditions.users.excludeGroups = groupsCreated.ToArray();
                    conditionalAccessPolicy.displayName = conditionalAccessPolicy.displayName.Insert(0, policyPrefix).Replace("<PREFIX> -","");

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
            signalR.sendMessage("Done#!");
            return new HttpStatusCodeResult(204);

        }

        public async Task<ActionResult> DeployBaseline()
        {
            List<String> dirs = new List<string>();

            String[] XmlFiles = Directory.GetDirectories(Server.MapPath("~/Content/PolicySets"));

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
            SignalRMessage signalR = new SignalRMessage();
            signalR.clientId = clientId;
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
    }
}