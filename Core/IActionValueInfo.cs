using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    /// <summary>
    /// 表示Action值的信息
    /// </summary>
    public interface IActionValueInfo
    {
        /// <summary>
        /// 获取或设置值的名称
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// 获取最终的值
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        object GetValue(IActionContext context);
    }
}
