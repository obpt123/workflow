using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class TraceContext:ITraceContext
    {
        public TraceContext(IActionContext context)
        {
            this.actionContext = context;
        }
        private IActionContext actionContext { get; set; }
        public int Depth { get; set; }

        public string ActionRef { get; set; }

        public string ActionName { get; set; }

        public Dictionary<string, object> InputValues { get; set; }

        public Dictionary<string, object> OutputValues { get; set; }

        public ActionResult Result { get; set; }

        public T GetService<T>()
        {
            return this.actionContext.GetService<T>();
        }
    }
}
