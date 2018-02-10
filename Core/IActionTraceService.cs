using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public interface IActionTraceService
    {
        void TraceResult(ITraceContext context);

    }
}
