using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class ExpressionValueInfo : IActionValueInfo
    {
        public string Name { get; set; }

        public string Expression { get; set; }

        public object GetValue(IActionContext context)
        {
            return null; ;
        }
    }
}
