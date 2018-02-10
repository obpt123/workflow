using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class WorkFlow : IAction
    {
        public ActionChain Setup { get; set; }
        public ActionChain Teardown { get; set; }
        public ActionChain Body { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            try
            {
                ChainUtility.Run(this.Setup, context);
                ChainUtility.Run(this.Body, context);
            }
            finally
            {
                ChainUtility.Run(this.Teardown, context);
            }
            return ActionResult.FromContext(context);
        }
    }

}
