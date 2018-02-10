using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows.Workflow.Actions
{
    public class While : IAction, IChainEntry
    {
        public ActionChain Entry
        {
            get;set;
        }

        public string Condition { get; set; }

        public ActionResult Exec(IActionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
