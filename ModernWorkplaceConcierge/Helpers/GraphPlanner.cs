using Microsoft.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphPlanner : GraphClient
    {
        private string clientId;
        private GraphServiceClient graphServiceClient;
        private SignalRMessage signalRMessage;

        public GraphPlanner(string clientId)
        {
            this.clientId = clientId;
            this.signalRMessage = new SignalRMessage(clientId);
            this.graphServiceClient = GetAuthenticatedClient();
        }

        public async Task<PlannerBucket> AddPlannerBucketAsync(PlannerBucket plannerBucket)
        {
            signalRMessage.sendMessage($"POST: {graphServiceClient.Planner.Buckets.Request().RequestUrl}");
            var response = await graphServiceClient.Planner.Buckets.Request().AddAsync(plannerBucket);
            return response;
        }

        public async Task<PlannerTask> AddPlannerTaskAsync(PlannerTask plannerTask)
        {
            signalRMessage.sendMessage($"POST: {graphServiceClient.Planner.Tasks.Request().RequestUrl}");
            var response = await graphServiceClient.Planner.Tasks.Request().AddAsync(plannerTask);
            return response;
        }

        public async Task<PlannerTaskDetails> AddPlannerTaskDetailsAsync(PlannerTaskDetails plannerTaskDetails, string taskId)
        {
            signalRMessage.sendMessage($"GET: {graphServiceClient.Planner.Tasks[taskId].Details.Request().RequestUrl}");
            var originalTaskDescription = await graphServiceClient.Planner.Tasks[taskId].Details.Request().GetAsync();

            signalRMessage.sendMessage($"PATCH: {graphServiceClient.Planner.Tasks[taskId].Details.Request().Header("If-Match", originalTaskDescription.GetEtag()).RequestUrl}");
            var response = await graphServiceClient.Planner.Tasks[taskId].Details.Request().Header("If-Match", originalTaskDescription.GetEtag()).UpdateAsync(plannerTaskDetails);
            return response;
        }

        public async Task<IEnumerable<PlannerBucket>> GetPlannerBucketsAsync(string planId)
        {
            signalRMessage.sendMessage($"GET: {graphServiceClient.Planner.Plans[planId].Buckets.Request().RequestUrl}");
            var response = await graphServiceClient.Planner.Plans[planId].Buckets.Request().GetAsync();
            return response.CurrentPage;
        }

        public async Task<PlannerPlan> GetplannerPlanAsync(string id)
        {
            signalRMessage.sendMessage($"GET: {graphServiceClient.Planner.Plans[id].Request().RequestUrl}");
            var response = await graphServiceClient.Planner.Plans[id].Request().GetAsync();
            return response;
        }

        public async Task<IEnumerable<PlannerPlan>> GetplannerPlansAsync()
        {
            signalRMessage.sendMessage($"GET: {graphServiceClient.Me.Planner.Plans.Request().RequestUrl}");
            var response = await graphServiceClient.Me.Planner.Plans.Request().GetAsync();
            return response.CurrentPage;
        }

        public async Task<PlannerTaskDetails> GetPlannerTaskDetailsAsync(string taskId)
        {
            signalRMessage.sendMessage($"GET: {graphServiceClient.Planner.Tasks[taskId].Details.Request().RequestUrl}");
            var response = await graphServiceClient.Planner.Tasks[taskId].Details.Request().GetAsync();
            return response;
        }
    }
}