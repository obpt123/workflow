using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    /// <summary>
    /// Action 执行的上下文
    /// </summary>
    public interface IActionContext:IDictionary<string,object>
    {

    }
    /// <summary>
    /// 表示开关
    /// </summary>
    public interface ISwitch
    {
        bool CanContinue(IActionContext context);
    }
    /// <summary>
    /// 表示Action的执行结果
    /// </summary>
    public class ActionResult
    {
        /// <summary>
        /// 执行过程的错误
        /// </summary>
        public Exception Error { get; set; }
        /// <summary>
        /// 执行结果
        /// </summary>
        public object Result { get; set; }

    }
    /// <summary>
    /// 表示Action的接口
    /// </summary>
    public interface IAction
    {
        ActionResult Exec(IActionContext context);
    }
    /// <summary>
    /// 表示Action的执行入口
    /// </summary>
    public interface IActionEntry
    {
        IAction Action { get; set; }

        ActionArgumentList Arguments { get; set; }
    }
    /// <summary>
    /// 表示Action的参数信息
    /// </summary>
    public class ActionArgument
    {
        public string Name { get; set; }

        public IActionValueDesc ValueDesc { get; set; }

    }
    /// <summary>
    /// 表示Action参数值的描述
    /// </summary>
    public interface IActionValueDesc
    {
        object GetValue(IActionContext context);
    }
    /// <summary>
    /// 表示Action的参数列表
    /// </summary>
    public class ActionArgumentList : List<ActionArgument>
    {

    }
    public class ActionChain
    {
        public string Name { get; set; }
        public IAction Action { get; set; }
        public ActionChainGroup OnSuccess { get; set; }

        public ActionChainGroup OnErrors { get; set; }

        public ActionChainGroup OnCompleted { get; set; }
    }
    public class ActionChainWrapper
    {
        public ISwitch Switch { get; set; }

        public ActionChain ActionChain { get; set; }
    }
    public class ActionChainGroup
    {
        public SwitchKind SwitchKind { get; set; }

        List<ActionChainWrapper> Actions { get; set; }
    }

    public class WorkFlow : IAction
    {
        public ActionChainGroup Actions { get; private set; }
        public ActionResult Exec(IActionContext context)
        {
            //this.Actions.RunGroup(context);
            return ActionResult.FromContext(context);
        }
    }



    public class Loop:IAction
    {
        public IEnumerable Source { get; set; }
        public ActionChainGroup Actions { get; private set; }

        public ActionResult Exec(IActionContext context)
        {
            if (Source != null)
            {
                foreach (var item in Source)
                {
                   // this.Actions.RunGroup(context);
                }
            }

            return ActionResult.FromContext(context);
        }
    }



    public enum SwitchKind
    {
        /// <summary>
        /// 单条分支
        /// </summary>
        Single,
        /// <summary>
        /// 多条分支
        /// </summary>
        Mutile
    }

    public interface IActionRunner
    {
        ActionResult RunAction(IActionEntry actionEntry, IActionContext context);
    }


}
