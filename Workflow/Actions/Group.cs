using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class Group : IAction, IChainEntry
    {
        public ActionChain Entry { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            return null;
        }
    }
}
