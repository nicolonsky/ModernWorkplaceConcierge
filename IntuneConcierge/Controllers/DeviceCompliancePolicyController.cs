using IntuneConcierge.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace IntuneConcierge.Controllers
{
    public class DeviceCompliancePolicyController : BaseController
    {
        // GET: DeviceCompliancePolicy
        [Authorize]
        public async Task<ActionResult> Index()
        {

            var deviceconfigs = await GraphHelper.GetDeviceCompliancePoliciesAsync();
            return View(deviceconfigs);
        }
    }
}