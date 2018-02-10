using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    /// <summary>
    /// 提供获取服务的接口
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// 根据泛型类型获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetService<T>();
    }
}
