using Microsoft.AspNet.SignalR;

namespace ModernWorkplaceConcierge.Helpers
{
    public class SignalRMessage
    {
        public string clientId { get; set; }

        public SignalRMessage(string clientId)
        {
            this.clientId = clientId;
        }

        public void sendMessage(string message)
        {
            if ((!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(this.clientId)))
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
                hubContext.Clients.Client(this.clientId).addMessage(message);
            }
        }
    }
}