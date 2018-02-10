using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    /// <summary>
    /// 表示Action的接口
    /// </summary>
    public interface IAction
    {
        ActionResult Exec(IActionContext context);
    }
}
