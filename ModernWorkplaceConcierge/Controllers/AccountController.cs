// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using ModernWorkplaceConcierge.TokenStorage;
using System.Configuration;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace ModernWorkplaceConcierge.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                // Signal OWIN to send an authorization request to Azure
                Request.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        public ActionResult SignOut()
        {
            if (Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.SignOut(
                new AuthenticationProperties { RedirectUri = ConfigurationManager.AppSettings["RedirectUri"] },
                OpenIdConnectAuthenticationDefaults.AuthenticationType,
                CookieAuthenticationDefaults.AuthenticationType);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}