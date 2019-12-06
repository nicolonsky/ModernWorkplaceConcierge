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

                            string response = await GraphHelper.ImportCaConfig(result);

                            Message("Success", response);
                        }
                        catch (Exception e)
                        {
                            Flash(e.Message);
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

                                                    string response = await GraphHelper.ImportCaConfig(result);

                                                    if (!(String.IsNullOrEmpty(response)))
                                                    {
                                                        Message("Success", response);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Flash(e.ToString());
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Flash(e.Message);
                    }
                }
                else if (files.Length > 0)
                {
                    Flash("Unsupported file type", files[0].FileName);
                }
            }
            catch (NullReferenceException)
            {
                Flash("Please select a file!");
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