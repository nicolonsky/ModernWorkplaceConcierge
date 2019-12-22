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

namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    public class PlannerController : BaseController
    {
        // GET: Planner
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            // Get all plans

            var plans = await GraphHelper.GetplannerPlans();

            return View(plans);
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> Import(HttpPostedFileBase file, string PlannerPlan)
        {
            // Load JSON

            Message("Selected Planner Plan:" + PlannerPlan);

            BinaryReader b = new BinaryReader(file.InputStream);
            byte[] binData = b.ReadBytes(file.ContentLength);
            string result = Encoding.UTF8.GetString(binData);


            JsonReader reader = new JsonTextReader(new StringReader(result));
            reader.DateParseHandling = DateParseHandling.None;
            reader.DateTimeZoneHandling = DateTimeZoneHandling.Unspecified;
            JObject trelloBoard = JObject.Load(reader);


            foreach (JToken task in trelloBoard.SelectToken("cards"))
            {
                //Flash(JsonConvert.SerializeObject(task));

                string dueDateTime = (string)task["due"];

                string assignedUser = "";

                try
                {

                    assignedUser = (string)task.SelectToken("member/name");
                }
                catch { }

                if (!string.IsNullOrEmpty(assignedUser))
                {
                    Message(assignedUser);

                }

                

                //User user = await GraphHelper.GetUser((string)task["memberCreator/fullName"]);

                PlannerTask plannerTask = new PlannerTask
                {
                    PlanId = PlannerPlan,
                    Title = (string)task["name"],
                    
                };

                if (!string.IsNullOrEmpty(dueDateTime))
                {
                    plannerTask.DueDateTime = DateTimeOffset.Parse(dueDateTime);
                }

                // Add the task
                var request = await GraphHelper.AddPlannerTask(plannerTask);

                string attachmentName = (string)task.SelectToken("attachments[0].name");
                string attachmentUrl = (string)task.SelectToken("attachments[0].url");
 
                string taskDescription = (string)task["desc"];

                if (!string.IsNullOrEmpty(taskDescription) || !string.IsNullOrEmpty(attachmentUrl))
                {

                    PlannerTaskDetails plannerTaskDetails = new PlannerTaskDetails();

                    plannerTaskDetails.Description = taskDescription;

                    if (!string.IsNullOrEmpty(attachmentUrl) && !string.IsNullOrEmpty(attachmentName))
                    {
                        plannerTaskDetails.References.AddReference(attachmentUrl, attachmentName);
                    }

                    try
                    {
                        
                        var response = await GraphHelper.AddPlannerTaskDetails(plannerTaskDetails, request.Id);

                    }
                    catch (Exception e)
                    {
                        Flash(e.Message);
                    }  
                }
               // Message(JsonConvert.SerializeObject(request, Formatting.Indented));

            }

            return RedirectToAction("Index");
        }
    }
}