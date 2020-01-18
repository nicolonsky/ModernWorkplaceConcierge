using ModernWorkplaceConcierge.Helpers;
using System;
using System.IO;
using System.IO.Compression;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Microsoft.Graph;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Mvc;


namespace ModernWorkplaceConcierge.Controllers
{
    [System.Web.Mvc.Authorize]
    public class IntuneController : BaseController
    {
        public ActionResult Import()
        {
           
            return View();
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> Upload(HttpPostedFileBase[] files, string clientId)
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

                            string response = await GraphHelper.AddIntuneConfig(result, clientId);
                            signalR.sendMessage("Success " + response);

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
                                            if (entry.FullName.Contains("WindowsAutopilotDeploymentProfile") || entry.FullName.Contains("DeviceConfiguration") || entry.FullName.Contains("DeviceCompliancePolicy") || entry.FullName.Contains("DeviceManagementScript") || entry.FullName.Contains("ManagedAppPolicy"))
                                            {
                                                using (var unzippedEntryStream = entry.Open())
                                                {
                                                    using (var ms = new MemoryStream())
                                                    {
                                                        unzippedEntryStream.CopyTo(ms);
                                                        var unzippedArray = ms.ToArray();
                                                        string result = Encoding.UTF8.GetString(unzippedArray);

                                                        if (!string.IsNullOrEmpty(result))
                                                        {
                                                            string response = await GraphHelper.AddIntuneConfig(result, clientId);

                                                            if (!(string.IsNullOrEmpty(response)))
                                                            {
                                                                signalR.sendMessage("Success " +  response);
                                                            }
                                                        }
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
                    signalR.sendMessage("Error unsupported file: " + files[0].FileName);
                }
            }
            catch (Exception e) {
                signalR.sendMessage("Error: " + e.Message);
            }

            signalR.sendMessage("Done#!");
            return new HttpStatusCodeResult(204);
        }

        // GET: Export
        public ActionResult Index()
        {
            return View();
        }

        // GET: Export
        public async System.Threading.Tasks.Task<ViewResult> Roles()
        {
            try
            {
                var roleDefinitions = await GraphHelper.GetRoleDefinitions();

                return View(roleDefinitions);
            }
            catch (Exception e)
            {
                Flash(e.Message, e.StackTrace.ToString());
                return View();
            } 
        }

        public async System.Threading.Tasks.Task<ActionResult> Copy(string Id)
        {
            try
            {
                var role = await GraphHelper.CopyRoleDefinition(Id);

                return RedirectToAction("Roles");

            }
            catch (Exception e)
            {
                Flash("Error getting DeviceManagementScripts" + e.Message.ToString());

                return RedirectToAction("Roles");
            }
        }

        public async System.Threading.Tasks.Task<ViewResult> DeviceManagementScripts()
        {
            try
            {
                var scripts = await GraphHelper.GetDeviceManagementScriptsAsync();

                return View(scripts);

            }
            catch (Exception e)
            {
                Flash("Error getting DeviceManagementScripts" + e.Message.ToString());

                return View();
            }
        }

        public async System.Threading.Tasks.Task<PartialViewResult>PowerShellScriptContent(string Id)
        {
            try
            {
                var scripts = await GraphHelper.GetDeviceManagementScriptAsync(Id);

                string powerShellCode = Encoding.UTF8.GetString(scripts.ScriptContent);

                return PartialView("_PowerShellScriptContent", powerShellCode);

            }
            catch (Exception e)
            {
                Flash("Error getting DeviceManagementScripts" + e.Message.ToString());

                return PartialView();
            }
        }

        public async System.Threading.Tasks.Task<FileResult> DownloadDeviceManagementScript(String Id)
        {
            DeviceManagementScript script = await GraphHelper.GetDeviceManagementScriptAsync(Id);

            return File(script.ScriptContent, "text/plain", script.FileName);

        }
    }
}