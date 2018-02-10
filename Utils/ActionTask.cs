using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public static class ActionTask
    {
        public static ActionResult Run(ActionEntry entry, List<IActionValueInfo> inputs, IActionContext context)
        {
            var arguments = ActionUtility.GetInputArguments(entry, inputs, context);
            ActionUtility.SetInputArguments(entry, arguments, context);
            return entry.Action.Exec(context);

        }
    }
}
