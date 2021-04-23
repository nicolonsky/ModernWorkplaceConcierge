using Microsoft.Graph;
using ModernWorkplaceConcierge.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

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
                GraphPlanner graphPlanner = new GraphPlanner(null);
                var plans = await graphPlanner.GetplannerPlansAsync();

                return View(plans);
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> Import(HttpPostedFileBase file, string PlannerPlan, string clientId)
        {
            SignalRMessage signalR = new SignalRMessage(clientId);
            GraphPlanner graphPlanner = new GraphPlanner(clientId);
            try
            {
                // Get current planner object
                var planner = await graphPlanner.GetplannerPlanAsync(PlannerPlan);

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
                    // check if list was archived
                    bool isOpen = !(bool)list["closed"];

                    if (!bucketsToCreate.Contains(bucketName) && isOpen)
                    {
                        bucketsToCreate.Add(bucketName);
                    }
                }

                // Get existing planner buckets
                IEnumerable<PlannerBucket> plannerBuckets = await graphPlanner.GetPlannerBucketsAsync(PlannerPlan);

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

                            var reponse = await graphPlanner.AddPlannerBucketAsync(plannerBucket);
                        }
                    }
                    catch
                    {
                    }
                }

                // Get available planner buckets
                plannerBuckets = await graphPlanner.GetPlannerBucketsAsync(PlannerPlan);

                // create tasks
                foreach (JToken task in trelloBoard.SelectToken("cards"))
                {
                    try
                    {
                        // Get name of the trello list which will become a planner bucket
                        string trelloId = (string)task["idList"];
                        string name = (string)trelloBoard.SelectToken($"$.lists[?(@.id == '{trelloId}')]")["name"];
                        // Check if task is in an archived list --> won't be imported
                        bool isInArchivedList = (bool)trelloBoard.SelectToken($"$.lists[?(@.id == '{trelloId}')]")["closed"];

                        PlannerTask plannerTask = new PlannerTask
                        {
                            PlanId = PlannerPlan,
                            Title = (string)task["name"],
                        };

                        if (isInArchivedList)
                        {
                            signalR.sendMessage("Discarding task because stored in an archived list: '" + plannerTask.Title + "'");
                        }
                        else
                        {
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
                               
                                // workaround: https://github.com/nicolonsky/ModernWorkplaceConcierge/issues/75#issuecomment-821622465
                                plannerTask.Assignments.ODataType = null;
                                
                                foreach (JToken currentUser in assignedToId)
                                {
                                    if (!string.IsNullOrEmpty((string)currentUser))
                                    {
                                        string assignedToname = (string)trelloBoard.SelectToken($"$.members[?(@.id == '{(string)currentUser}')]")["fullName"];

                                        User user = await GraphHelper.GetUser(assignedToname);

                                        plannerTask.Assignments.AddAssignee(user.Id);
                                    }
                                }
                            }
                            catch
                            {
                            }

                            // Add the task
                            var request = await graphPlanner.AddPlannerTaskAsync(plannerTask);

                            signalR.sendMessage("Successfully imported task '" + request.Title + "'");

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
                                        try
                                        {
                                            plannerTaskDetails.References.AddReference(attachmentUrl, attachmentName);
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }

                                try
                                {
                                    plannerTaskDetails.Checklist = new PlannerChecklistItems();

                                    JToken[] checklists = task.SelectTokens("idChecklists[*]").ToArray();

                                    foreach (JToken checklist in checklists)
                                    {
                                        JToken[] checklistItems = trelloBoard.SelectTokens($"$.checklists[?(@.id == '{(string)checklist}')].checkItems[*].name").ToArray();

                                        int checklistCount = 0;

                                        foreach (JToken checklistItem in checklistItems)
                                        {
                                            string checklistItemName = (string)checklistItem;

                                            // truncate string because checklist items are limited to 100 characters
                                            if (checklistItemName.Length >= 100)
                                            {
                                                signalR.sendMessage("Truncating checklist item: '" + checklistItemName + "' on task: '" + plannerTask.Title + "'. The maximum length in Planner is 100 characters!");
                                                checklistItemName = checklistItemName.Substring(0, 100);
                                            }

                                            if (!(checklistCount >= 20))
                                            {
                                                plannerTaskDetails.Checklist.AddChecklistItem(checklistItemName);
                                            }
                                            else
                                            {
                                                signalR.sendMessage("Discarding checklist item: '" + checklistItemName + "' on task: '" + plannerTask.Title + "' because Planner limit's each card to 20 checklist items!");
                                            }

                                            checklistCount++;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    signalR.sendMessage("Error: " + e.Message);
                                }

                                var response = await graphPlanner.AddPlannerTaskDetailsAsync(plannerTaskDetails, request.Id);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        signalR.sendMessage("Error: " + e.Message);
                    }
                }

                signalR.sendMessage("Success imported: " + importedTasksCounter + " tasks to planner: " + planner.Title);
            }
            catch (Exception e)
            {
                signalR.sendMessage("Error: " + e.Message);
            }

            signalR.sendMessage("Done#!");
            return new HttpStatusCodeResult(204);
        }
    }
}
