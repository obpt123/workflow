using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    /// <summary>
    /// Action 执行的上下文
    /// </summary>
    public interface IActionContext : IDisposable, IServiceProvider
    {
        int Depth { get; }
        IActionContext Parent { get; }
        Dictionary<string, object> Inputs { get; set; }
        Dictionary<string, object> Vars { get; set; }
        List<IActionContext> SubContexts { get; }
        IActionContext BeginContext();

        bool ReleaseContext(IActionContext context);

        Dictionary<string, object> GetContextValues();

    }
}
