using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public interface IActionFactory
    {
        ActionEntry GetAction(string actionRef);
    }
}
