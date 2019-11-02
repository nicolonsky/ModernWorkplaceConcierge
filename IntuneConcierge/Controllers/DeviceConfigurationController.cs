// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using IntuneConcierge.Helpers;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace IntuneConcierge.Controllers
{
    public class DeviceConfigurationController : BaseController
    {
        // GET: Calendar
        [Authorize]
        public async Task<ActionResult> Index()
        {

            var deviceconfigs = await GraphHelper.GetDeviceConfigurationsAsync();

            return View(deviceconfigs);
        }

        // GET: Calendar
        [Authorize]
        public async Task<ActionResult> Details()
        {

            var deviceconfigs = await GraphHelper.GetDeviceConfigurationsAsync();

            return View(deviceconfigs);
        }
    }
}