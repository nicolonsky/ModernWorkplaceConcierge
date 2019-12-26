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
            try {

                // Get current planner object
                var planner = await GraphHelper.GetplannerPlan(PlannerPlan);
                
                // Count imported tasks
                int importedTasksCounter = 0;

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

                      

                        PlannerTask plannerTask = new PlannerTask
                        {
                            PlanId = PlannerPlan,
                            Title = (string)task["name"],
                        };

                        try
                        {
                            // Get bucketId to store tasks
                            string bucketId = plannerBuckets.Where(p => p.Name.Equals(name)).First().Id;
                            plannerTask.BucketId = bucketId;
                        }
                        catch
                        {
                        }

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
                        try
                        {
                            JToken[] assignedToId = task.SelectTokens("idMembers[*]").ToArray();

                            plannerTask.Assignments = new PlannerAssignments();

                            foreach (JToken currentUser in assignedToId)
                            {
                                if (!string.IsNullOrEmpty((string)currentUser))
                                {
                                    string assignedToname = (string)trelloBoard.SelectToken($"$.members[?(@.id == '{(string)currentUser}')]")["fullName"];

                                    User user = await GraphHelper.GetUser(assignedToname);

                                    plannerTask.Assignments.AddAssignee(user.Id);

                                }
                            }
                        }catch
                        {
                        }

                        // Add the task
                        var request = await GraphHelper.AddPlannerTask(plannerTask);
                        importedTasksCounter++;

                        // Add task details like description and attachments

                        JToken[] attachments = task.SelectTokens("attachments[*]").ToArray();
                        string taskDescription = (string)task["desc"];

                        if (!string.IsNullOrEmpty(taskDescription) || attachments.Count() > 0)
                        {
                            PlannerTaskDetails plannerTaskDetails = new PlannerTaskDetails();

                            if (!string.IsNullOrEmpty(taskDescription))
                            {

                                plannerTaskDetails.Description = taskDescription;
                            }

                            plannerTaskDetails.References = new PlannerExternalReferences();

                            foreach (JToken attachment in attachments)
                            {
                                string attachmentUrl = attachment.Value<string>("url");
                                string attachmentName = attachment.Value<string>("name");

                                if (!string.IsNullOrEmpty(attachmentUrl))
                                {
                                    try {
                                        plannerTaskDetails.References.AddReference(attachmentUrl, attachmentName);
                                    }
                                    catch
                                    {
                                    } 
                                }
                            }

                            var response = await GraphHelper.AddPlannerTaskDetails(plannerTaskDetails, request.Id);
                        }
                    }
                    catch (Exception e)
                    {
                        Flash(e.Message, e.StackTrace);
                    }
                }

                Message("Imported: " + importedTasksCounter + " tasks to planner: " + planner.Title);
            }
            catch (Exception e)
            {
                Flash(e.Message);
            }
            return RedirectToAction("Index");
        }
    }
}