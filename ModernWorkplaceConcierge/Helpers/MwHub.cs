using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
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