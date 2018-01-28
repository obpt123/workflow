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
    public interface IActionContext //: IDictionary<string, object>
    {
        IDictionary<string, object> Inputs { get; set; }
        IDictionary<string,object> Vars { get; set; }
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
        string ActionRef { get; set; }
        List<ActionArgument> Arguments { get; set; }
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
    public class ActionChain
    {
        public string Name { get; set; }
        public IActionEntry Action { get; set; }
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
        public ActionChain Action { get; private set; }


        public ActionResult Exec(IActionContext context)
        {
            //this.Actions.RunGroup(context);
            // return ActionResult.FromContext(context);
            return null;
        }
    }




    public class Loop : IAction
    {
        public IEnumerable Source { get; set; }
        public ActionChainGroup Actions { get; private set; }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ActionResult Exec(IActionContext context)
        {
            if (Source != null)
            {
                foreach (var item in Source)
                {
                    // this.Actions.RunGroup(context);
                }
            }

            return null;
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
        IMetaDataService MetaDataService { get; set; }
        ActionResult RunAction(IActionEntry actionEntry, IActionContext context);
    }
    public class ActionRunner : IActionRunner
    {
        static ActionRunner()
        {
            Default = new ActionRunner();
        }
        public static IActionRunner Default { get; private set; }
        public IMetaDataService MetaDataService { get; set; }
        public IActionBuilder ActionBuildService { get; set; }
        public ActionResult RunAction(IActionEntry actionEntry, IActionContext context)
        {
            try
            {
                var meta = this.GetActionMeta(actionEntry);
                var action = this.CreateAction(meta);
                this.SetInputArguments(meta, action, actionEntry, context);
                return action.Exec(context);
            }
            catch (Exception ex)
            {
                return new ActionResult()
                {
                    Error = ex
                };
            }
        }

        private ActionMeta GetActionMeta(IActionEntry actionEntry)
        {
            return this.MetaDataService.GetMetaData(actionEntry.ActionRef);
        }
        private IAction CreateAction(ActionMeta meta)
        {
            return this.ActionBuildService.BuildAction(meta.ContentType, meta.Content);
        }
        private void SetInputArguments(ActionMeta meta, IAction action, IActionEntry actionEntry, IActionContext context)
        {
            Dictionary<string, object> inputValues = new Dictionary<string, object>();
            foreach (var inputMeta in meta.Inputs ?? new List<ActionInputMeta>())
            {
                var input = actionEntry.Arguments.SingleOrDefault(p => p.Name == inputMeta.Name);
                if (input == null)
                {
                    if (inputMeta.DefaultValue == null)
                    {
                        if (inputMeta.IsRequired)
                        {
                            throw new ActionException($"参数[{input.Name}]是必须的。");
                        }
                    }
                    else
                    {
                        inputValues.Add(inputMeta.Name,Convert.ChangeType(inputMeta.DefaultValue,inputMeta.Type));
                    }
                }
                else
                {
                    var value = input.ValueDesc.GetValue(context);
                    inputValues.Add(inputMeta.Name, Convert.ChangeType(value,inputMeta.Type));
                }
            }
            if (meta.Kind == ActionKind.Action)
            {
                foreach (var kv in inputValues)
                {
                    action.GetType().GetProperty(kv.Key).SetValue(action, kv.Value,null);
                }
            }
            else
            {
                context.Inputs = inputValues;
            }
        }
    }


    [Serializable]
    public class ActionException : Exception
    {
        public ActionException() { }
        public ActionException(string message) : base(message) { }
        public ActionException(string message, Exception inner) : base(message, inner) { }
        protected ActionException(
          Runtime.Serialization.SerializationInfo info,
          Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
