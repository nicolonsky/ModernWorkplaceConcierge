using System;
using System.Net;
using System.Web.Mvc;

namespace ModernWorkplaceConcierge.Helpers
{
    public class CustomAuthorization : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectResult("/Home/Error?message=Unauthorized&debug=You are not authorized to use this function");
            }
            else
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}