using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModernWorkplaceConcierge.Helpers
{
    public class UserConnectionManager
    {
        private static Dictionary<string, List<string>> userConnectionMap = new Dictionary<string, List<string>>();
        private static string userConnectionMapLocker = string.Empty;//find away to avoid lock later

        public void KeepUserConnection(string userId, string connectionId)
        {
            lock (userConnectionMapLocker)
            {
                if (!userConnectionMap.ContainsKey(userId))
                {
                    userConnectionMap[userId] = new List<string>();
                }
                userConnectionMap[userId].Add(connectionId);
            }
        }

        public void RemoveUserConnection(string connectionId)
        {
            //Remove the connectionId of user 
            lock (userConnectionMapLocker)
            {
                foreach (var userId in userConnectionMap.Keys)
                {
                    if (userConnectionMap.ContainsKey(userId))
                    {
                        if (userConnectionMap[userId].Contains(connectionId))
                        {
                            userConnectionMap[userId].Remove(connectionId);
                            break;
                        }
                    }
                }
            }
        }
        public List<string> GetUserConnections(string userId)
        {

            var conn = new List<string>();
            lock (userConnectionMapLocker)
            {
                conn = userConnectionMap[userId];
            }
            return conn;
        }
    }
}
