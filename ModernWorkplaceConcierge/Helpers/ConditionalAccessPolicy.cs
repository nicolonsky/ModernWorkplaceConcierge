using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModernWorkplaceConcierge.Helpers
{
    public class ConditionalAccessPolicy
    {
        public String DisplayName { get; set; }
        public String Id { get; set; }

        public ConditionalAccessPolicy (String displayName, String id)
        {
            this.DisplayName = displayName;
            this.Id = id;
        }
    }
}