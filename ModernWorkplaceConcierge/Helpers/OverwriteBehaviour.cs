using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ModernWorkplaceConcierge.Helpers
{
    public class OverwriteBehaviour
    {
        public string Name { get; set; }
        public string Behaviour { get; set; }

    }
    public class ViewModel
    {
        private readonly List<OverwriteBehaviour> overwriteBehaviours;

        [Display(Name = "Overwrite Behaviour")]
        public string SelectedBehaviour { get; set; }

        public IEnumerable<SelectListItem> SelectListItems
        {
            get { return new SelectList(overwriteBehaviours, "Name", "Behaviour"); }
        }
    }
}