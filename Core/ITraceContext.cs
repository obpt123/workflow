using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    /// <summary>
    /// 表示执行跟踪
    /// </summary>
    public interface ITraceContext:IServiceProvider
    {
        int Depth { get; }

        string ActionRef { get; }

        string ActionName { get;  }

        Dictionary<string, object> InputValues { get;  }

        Dictionary<string, object> OutputValues { get;  }

        ActionResult Result { get;  }
    }
}
