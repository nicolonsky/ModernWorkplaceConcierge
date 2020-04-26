using Microsoft.AspNet.SignalR;

namespace ModernWorkplaceConcierge.Helpers
{
    public class MwHub : Hub
    {
        public void SendMessage(string message)
        {
            Clients.Caller.addMessage(message);  // Message sent
        }
    }
}