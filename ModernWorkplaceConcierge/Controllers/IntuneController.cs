using Microsoft.Graph;
using ModernWorkplaceConcierge.Helpers;
using ModernWorkplaceConcierge.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ModernWorkplaceConcierge.Controllers
{
    [System.Web.Mvc.Authorize]
    public class IntuneController : BaseController
    {
        private List<string> supportedFolders = new List<string>();

        public ActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> Upload(HttpPostedFileBase[] files, OverwriteBehaviour overwriteBehaviour, string clientId)
        {
            SignalRMessage signalR = new SignalRMessage(clientId);

            supportedFolders.Add("WindowsAutopilotDeploymentProfile");
            supportedFolders.Add("DeviceConfiguration");
            supportedFolders.Add("DeviceCompliancePolicy");
            supportedFolders.Add("DeviceManagementScript");
            supportedFolders.Add("ManagedAppPolicy");
            supportedFolders.Add("RoleScopeTags");

            try
            {
                GraphIntuneImport graphIntuneImport = new GraphIntuneImport(clientId, overwriteBehaviour);

                if (files.Length > 0 && files[0].FileName.Contains(".json"))
                {
                    foreach (HttpPostedFileBase file in files)
                    {
                        try
                        {
                            BinaryReader b = new BinaryReader(file.InputStream);
                            byte[] binData = b.ReadBytes(file.ContentLength);
                            string result = Encoding.UTF8.GetString(binData);
                            await graphIntuneImport.AddIntuneConfig(result);
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
                                            if (supportedFolders.Contains(entry.FullName))
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
                                                            await graphIntuneImport.AddIntuneConfig(result);
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
            catch (Exception e)
            {
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

        public async System.Threading.Tasks.Task<ViewResult> DeviceManagementScripts()
        {
            try
            {
                GraphIntune graphIntune = new GraphIntune(null);
                var scripts = await graphIntune.GetDeviceManagementScriptsAsync();
                return View(scripts);
            }
            catch (Exception e)
            {
                Flash("Error getting DeviceManagementScripts" + e.Message.ToString());
                return View();
            }
        }

        public async System.Threading.Tasks.Task<ViewResult> Win32AppDetectionScripts()
        {
            try
            {
                GraphIntune graphIntune = new GraphIntune(null);
                var apps = await graphIntune.GetWin32MobileAppsAsync();

                List<Win32LobApp> win32LobApps = new List<Win32LobApp>();

                foreach (Win32LobApp app in apps)
                {
                    var details = await graphIntune.GetWin32MobileAppAsync(app.Id);

                    if (details.DetectionRules.Any(d => d is Win32LobAppPowerShellScriptDetection))
                    {
                        win32LobApps.Add(details);
                    }
                }

                return View(win32LobApps);
            }
            catch (Exception e)
            {
                Flash("Error " + e.Message.ToString());
                return View();
            }
        }

        public async System.Threading.Tasks.Task<PartialViewResult> Win32AppPsDetectionScriptContent(string Id)
        {
            try
            {
                GraphIntune graphIntune = new GraphIntune(null);
                var script = await graphIntune.GetWin32MobileAppPowerShellDetectionRuleAsync(Id);
                string powerShellCode = Encoding.UTF8.GetString(Convert.FromBase64String(script.ScriptContent));
                return PartialView("_PowerShellDetectionScriptContent", powerShellCode);
            }
            catch (Exception e)
            {
                Flash("Error getting DeviceManagementScripts" + e.Message.ToString());
                return PartialView();
            }
        }

        public async System.Threading.Tasks.Task<FileResult> DownloadDetectionScript(string Id)
        {
            GraphIntune graphIntune = new GraphIntune(null);
            Win32LobApp win32LobApp = await graphIntune.GetWin32MobileAppAsync(Id);
            Win32LobAppPowerShellScriptDetection script = await graphIntune.GetWin32MobileAppPowerShellDetectionRuleAsync(Id);
            string fileName = $"{FilenameHelper.ProcessFileName(win32LobApp.DisplayName)}_detect.ps1";
            return File(Convert.FromBase64String(script.ScriptContent), "text/plain", fileName);
        }

        public async System.Threading.Tasks.Task<PartialViewResult> PowerShellScriptContent(string Id)
        {
            try
            {
                GraphIntune graphIntune = new GraphIntune(null);
                var scripts = await graphIntune.GetDeviceManagementScriptAsync(Id);
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
            GraphIntune graphIntune = new GraphIntune(null);
            DeviceManagementScript script = await graphIntune.GetDeviceManagementScriptAsync(Id);
            return File(script.ScriptContent, "text/plain", script.FileName);
        }

        //public async Task<ActionResult> ClearAll(bool confirm = false)
        //{
        //    GraphIntune graphIntune = new GraphIntune(null);
        //    if (confirm)
        //    {
        //        await graphIntune.ClearDeviceConfigurations();
        //    }
        //    return new HttpStatusCodeResult(204);
        //}
    }
}