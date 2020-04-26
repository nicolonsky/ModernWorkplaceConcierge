using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModernWorkplaceConcierge.Models
{
    public enum AADLicensePlan
    {
        FREE,
        PREMIUM_P1,
        PREMIUM_P2
    }

    public enum DeviceState
    {
        AAD_HYBRID,
        MEMCM,
        NOT_ENROLLED
    }

    public enum NetworkLocations
    {
        YES,
        NO
    }

    public enum AdditionalLicenses
    {
        YES,
        NO
    }
}