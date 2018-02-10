using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class ActionChainGroup
    {
        public SwitchKind SwitchKind { get; set; }

        public List<ActionChainWrapper> Actions { get; set; }
    }
}
