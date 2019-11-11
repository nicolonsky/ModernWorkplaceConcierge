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
        public async System.Threading.Tasks.Task<ActionResult> Upload(HttpPostedFileBase file)
        {
            try
            {
                BinaryReader b = new BinaryReader(file.InputStream);
                byte[] binData = b.ReadBytes(file.ContentLength);

                string result = System.Text.Encoding.UTF8.GetString(binData);

                ConditionalAccessPolicy conditionalAccessPolicy = JsonConvert.DeserializeObject<ConditionalAccessPolicy>(result);

                conditionalAccessPolicy.id = null;
                conditionalAccessPolicy.modifiedDateTime = null;

                string requestContent = JsonConvert.SerializeObject(conditionalAccessPolicy, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                bool res = await GraphHelper.AddConditionalAccessPolicyAsync(requestContent);

                b.Dispose();

            }
            catch (Exception e)
            {
                Flash(e.Message);

            }

            return RedirectToAction("Import");
        }

        public ViewResult Import()
        {

            return View();

        }

        /*
            CA policies: https://docs.microsoft.com/en-us/graph/api/conditionalaccessroot-list-policies?view=graph-rest-beta&tabs=http
                */

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

            byte[] capolicy = Encoding.GetEncoding(1250).GetBytes(JsonConvert.SerializeObject(conditionalAccessPolicy, Formatting.Indented).ToString());


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
                            byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                            var zipArchiveEntry = archive.CreateEntry(item.displayName + "_" + item.id + ".json", CompressionLevel.Fastest);

                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }
                    }

                    return File(ms.ToArray(), "application/zip", "Archive.zip");
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