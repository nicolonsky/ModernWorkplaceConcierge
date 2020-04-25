using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ModernWorkplaceConcierge.Models
{
    public enum OverwriteBehaviour
    {
        IMPORT_AS_DUPLICATE,
        DISCARD,
        OVERWRITE,
    }
}