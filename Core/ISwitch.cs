using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    /// <summary>
    /// 表示开关
    /// </summary>
    public interface ISwitch
    {
        /// <summary>
        /// 是否可以通过
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        bool CanContinue(IActionContext context);
    }
}
