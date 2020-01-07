using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;

namespace ModernWorkplaceConcierge.Helpers
{
    public class MwHub : Hub
    { 
        internal static void SendMessage(string message)
        {
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
            context.Clients.All.broadcast(DateTime.Now.ToString("o") + " " + message);
        }
    }
}