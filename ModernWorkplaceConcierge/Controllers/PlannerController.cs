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
using System.Collections;

namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    public class PlannerController : BaseController
    {
        // GET: Planner
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            try
            {
                // Get all plans
                var plans = await GraphHelper.GetplannerPlans();

                return View(plans);
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> Import(HttpPostedFileBase file, string PlannerPlan)
        {
            Message("Selected Planner Plan: " + PlannerPlan);

            // Get uploaded json
            BinaryReader b = new BinaryReader(file.InputStream);
            byte[] binData = b.ReadBytes(file.ContentLength);
            string result = Encoding.UTF8.GetString(binData);

            JsonReader reader = new JsonTextReader(new StringReader(result));
            // Do not parse datetime values 
            reader.DateParseHandling = DateParseHandling.None;
            reader.DateTimeZoneHandling = DateTimeZoneHandling.Unspecified;
            JObject trelloBoard = JObject.Load(reader);

            // Get trello lists
            ArrayList bucketsToCreate = new ArrayList();

            foreach (JToken list in trelloBoard.SelectToken("lists"))
            {
                string bucketName = (string)list["name"];

                if (!bucketsToCreate.Contains(bucketName))
                {
                    bucketsToCreate.Add(bucketName);
                }
            }

            // Get existing planner buckets
            IEnumerable<PlannerBucket> plannerBuckets = await GraphHelper.GetPlannerBuckets(PlannerPlan);

            // Create planner bucket if not exists
            foreach (string bucket in bucketsToCreate)
            {
                try
                {
                    if (!plannerBuckets.ToList().Where(p => p.Name == bucket).Any())
                    {
                        PlannerBucket plannerBucket = new PlannerBucket
                        {
                            Name = bucket,
                            PlanId = PlannerPlan
                        };

                        var reponse = await GraphHelper.AddPlannerBucket(plannerBucket);
                    }
                }
                catch
                {

                }
            }

            // Get available planner buckets
            plannerBuckets = await GraphHelper.GetPlannerBuckets(PlannerPlan);

            // create tasks
            foreach (JToken task in trelloBoard.SelectToken("cards"))
            {
                try
                {
                    // Get name of the trello list which will become a planner bucket
                    string trelloId = (string)task["idList"];
                    string name = (string)trelloBoard.SelectToken($"$.lists[?(@.id == '{trelloId}')]")["name"];

                    // Get bucketId to store tasks
                    string bucketId = plannerBuckets.Where(p => p.Name.Equals(name)).First().Id;

                    PlannerTask plannerTask = new PlannerTask
                    {
                        PlanId = PlannerPlan,
                        Title = (string)task["name"],
                        BucketId = bucketId
                    };

                    // Get completed
                    bool isClosed = bool.Parse((string)task["closed"]);

                    if (isClosed)
                    {
                        plannerTask.PercentComplete = 100;
                    }

                    // Get due
                    string dueDateTime = (string)task["due"];

                    if (!string.IsNullOrEmpty(dueDateTime))
                    {
                        plannerTask.DueDateTime = DateTimeOffset.Parse(dueDateTime);
                    }

                    // Get assigned user
                    string assignedToId = (string)task.SelectToken("idMembers[*]");

                    if (!string.IsNullOrEmpty(assignedToId))
                    {
                        string assignedToname = (string)trelloBoard.SelectToken($"$.members[?(@.id == '{assignedToId}')]")["fullName"];

                        User user = await GraphHelper.GetUser(assignedToname);

                        plannerTask.Assignments = new PlannerAssignments();
                        plannerTask.Assignments.AddAssignee(user.Id);

                    }

                    // Add the task
                    var request = await GraphHelper.AddPlannerTask(plannerTask);

                    // Add task details like description and attachments

                    string attachmentName = (string)task.SelectToken("attachments[*].name");
                    string attachmentUrl = (string)task.SelectToken("attachments[*].url");
                    string taskDescription = (string)task["desc"];

                    if (!string.IsNullOrEmpty(taskDescription) || !string.IsNullOrEmpty(attachmentUrl))
                    {
                        PlannerTaskDetails plannerTaskDetails = new PlannerTaskDetails();

                        if (!string.IsNullOrEmpty(taskDescription))
                        {

                            plannerTaskDetails.Description = taskDescription;
                        }

                        if (!string.IsNullOrEmpty(attachmentUrl) && !string.IsNullOrEmpty(attachmentName))
                        {
                            plannerTaskDetails.References = new PlannerExternalReferences();

                            plannerTaskDetails.References.AddReference(attachmentUrl, attachmentName);
                        }

                        var response = await GraphHelper.AddPlannerTaskDetails(plannerTaskDetails, request.Id);
                    }
                }
                catch (Exception e)
                {
                    Flash(e.Message, e.StackTrace);
                }  
            }

            return RedirectToAction("Index");
        }
    }
}