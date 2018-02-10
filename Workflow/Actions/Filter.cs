using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows.Workflow.Actions
{
    public class Filter : IAction
    {
        public IEnumerable Source { get; set; }
        public string Condition { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            return null;
        }
    }
}
