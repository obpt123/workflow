using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    /// <summary>
    /// 表示原值参数
    /// </summary>
    public class OriginalValueInfo : IActionValueInfo
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public object GetValue(IActionContext context)
        {
            return Value;
        }
    }
}
