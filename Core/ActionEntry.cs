using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Workflows.Meta;

namespace System.Workflows
{
    public sealed class ActionEntry
    {
        public ActionMeta Meta { get; set; }
        public IAction Action { get; set; }
    }
}
