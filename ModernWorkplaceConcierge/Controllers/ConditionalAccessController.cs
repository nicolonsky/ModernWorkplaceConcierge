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

namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    public class ConditionalAccessController : BaseController
    {
        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> Upload(HttpPostedFileBase[] files)
        {
            foreach (HttpPostedFileBase file in files)
            {
                try
                {
                    BinaryReader b = new BinaryReader(file.InputStream);
                    byte[] binData = b.ReadBytes(file.ContentLength);

                    string result = Encoding.UTF8.GetString(binData);

                    ConditionalAccessPolicy conditionalAccessPolicy = JsonConvert.DeserializeObject<ConditionalAccessPolicy>(result);

                    conditionalAccessPolicy.id = null;
                    conditionalAccessPolicy.state = "disabled";
                    conditionalAccessPolicy.createdDateTime = null;

                    string requestContent = JsonConvert.SerializeObject(conditionalAccessPolicy, new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented
                    });

                    try
                    {
                        var success = await GraphHelper.AddConditionalAccessPolicyAsync(requestContent);
                        Message("Success", success.ToString());
                    }
                    catch {

                        try
                        {
                            // remove Id's
                            conditionalAccessPolicy.conditions.users.includeUsers = new string[] { "none" };
                            conditionalAccessPolicy.conditions.users.excludeUsers = null;
                            conditionalAccessPolicy.conditions.users.includeGroups = null;
                            conditionalAccessPolicy.conditions.users.excludeGroups = null;
                            conditionalAccessPolicy.conditions.users.includeRoles = null;
                            conditionalAccessPolicy.conditions.users.excludeRoles = null;

                            conditionalAccessPolicy.conditions.applications.includeApplications = new string[] { "none" };
                            conditionalAccessPolicy.conditions.applications.excludeApplications = null;

                            requestContent = JsonConvert.SerializeObject(conditionalAccessPolicy, new JsonSerializerSettings()
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                                Formatting = Formatting.Indented
                            });

                            var success = await GraphHelper.AddConditionalAccessPolicyAsync(requestContent);

                            Message("Success: Unknown tenant ID's removed!", success.ToString());
                        }
                        catch (Exception e)
                        {

                            Flash(e.Message, e.StackTrace);
                        }
                    }
                }
                 catch{}
            }
            
            return RedirectToAction("Import");
        }

        public ViewResult Import()
        {

            return View();

        }

        // GET: ConditionalAccess
        public async System.Threading.Tasks.Task<ViewResult> Index()
        {
            try
            {
                var ca = await GraphHelper.GetConditionalAccessPoliciesAsync();

                ConditionalAccessPolicies policies = JsonConvert.DeserializeObject<ConditionalAccessPolicies>(ca);

                return View(policies.Value);

            }
            catch (Exception e)
            {
                Flash(e.Message);

            }

            return View();
        }

        public async System.Threading.Tasks.Task<FileResult> Download(String Id)
        {
            string ca = await GraphHelper.GetConditionalAccessPolicyAsync(Id);

            ConditionalAccessPolicy conditionalAccessPolicy = JsonConvert.DeserializeObject<ConditionalAccessPolicy>(ca);

            byte[] capolicy = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(conditionalAccessPolicy, Formatting.Indented).ToString());


            return File(capolicy, "application/json", "CA-Policy" + Id + ".json");
        }

        public async System.Threading.Tasks.Task<FileResult> DownloadAll()
        {
            try {
                string ca = await GraphHelper.GetConditionalAccessPoliciesAsync();

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
                Flash(e.Message);

            }

            return null;
        }
    }
}