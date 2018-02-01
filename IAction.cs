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
    public interface IActionContext:IDisposable 
    {
        IActionContext Parent { get; }
        Dictionary<string, object> Inputs { get; set; }
        Dictionary<string,object> Vars { get; set; }
        Dictionary<string,object> Outputs { get; set; }

        List<IActionContext> SubContexts { get; }
        IActionContext BeginContext();

        bool ReleaseContext(IActionContext context);
        
    }

    public class ActionContext : IActionContext
    {
        public Dictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Outputs { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Vars { get; set; } = new Dictionary<string, object>();
        public IActionContext Parent { get; private set; }
        public List<IActionContext> SubContexts { get; private set; } = new List<IActionContext>();


       

        public IActionContext BeginContext()
        {
            ActionContext context = new ActionContext();
            context.Parent = this;
            return context;
        }

        public bool ReleaseContext(IActionContext context)
        {
            return this.SubContexts.Remove(context);
        }

        void IDisposable.Dispose()
        {
            if (this.Parent != null)
            {
                this.Parent.ReleaseContext(this);
            }
        }
    }
    /// <summary>
    /// 表示开关
    /// </summary>
    public interface ISwitch
    {
        bool CanContinue(IActionContext context);
    }

    public class DefaultSwitch : ISwitch
    {
        public static ISwitch True = new DefaultSwitch(true);
        public static ISwitch False = new DefaultSwitch(false);

        public DefaultSwitch()
        {

        }
        public DefaultSwitch(bool val)
        {
            this.value = val;
        }
        private bool value;
        public bool CanContinue(IActionContext context)
        {
            return this.value;
        }
    }
    /// <summary>
    /// 表示Action的执行结果
    /// </summary>
    public class ActionResult
    {
        public ActionResult()
        {

        }
        public ActionResult(object result)
        {
            this.Result = result;
        }
        /// <summary>
        /// 执行过程的错误
        /// </summary>
        public Exception Error { get; set; }
        /// <summary>
        /// 执行结果
        /// </summary>
        public object Result { get; set; }

        public bool IsSuccess { get { return this.Error != null; } }

        public static ActionResult FromContext(IActionContext context)
        {
            return new ActionResult();
        }

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
        string Name { get; set; }
        string ActionRef { get; set; }
        List<ActionInput> Inputs { get; set; }

        IAction GetAction();
    }
    /// <summary>
    /// 表示Action的参数信息
    /// </summary>
    public class ActionInput
    {
        public string Name { get; set; }

        public IActionInputValueDesc ValueDesc { get; set; }

        public object GetValue(IActionContext context)
        {
            return this.ValueDesc.GetValue(context);
        }

    }
    public class ActionOutput
    {
        public string Name { get; set; }

        public IActionOutputValueDesc ValueDesc { get; set; }

        public void Output(ActionResult result, IActionContext context)
        {
            var value = this.ValueDesc.GetValue(result, context);

            context.Outputs[this.Name] = value;
        }

    }
    /// <summary>
    /// 表示Action参数值的描述
    /// </summary>
    public interface IActionInputValueDesc
    {
        object GetValue(IActionContext context);
    }
    public interface IActionOutputValueDesc
    {
        object GetValue(ActionResult result, IActionContext context);
    }
    public class ActionChain:IActionEntry
    {
        public string Name { get; set; }
        public ActionChainGroup OnSuccess { get; set; }
        public ActionChainGroup OnErrors { get; set; }
        public ActionChainGroup OnCompleted { get; set; }

        public string ActionRef { get; set; }

        public List<ActionInput> Inputs { get; set; }

        public List<ActionOutput> Outputs { get; set; }

        public IAction GetAction()
        {
            throw new NotImplementedException();
        }
    }
    public class ActionChainWrapper
    {
        public ISwitch Switch { get; set; }

        public ActionChain ActionChain { get; set; }
    }
    public class ActionChainGroup
    {
        public SwitchKind SwitchKind { get; set; }

        public List<ActionChainWrapper> Actions { get; set; }
    }

    public interface INewContext
    {

    }
    public class WorkFlow : IAction,INewContext
    {
        public ActionChain Entry { get;  set; }


        public ActionResult Exec(IActionContext context)
        {
            this.RunAction(Entry, context);
            return ActionResult.FromContext(context);
        }
        private void RunActionGroup(ActionChainGroup group, IActionContext context)
        {
            if (group == null|| group.Actions==null ) return;
            foreach (var chainWrap in group.Actions)
            {
                var @switch = chainWrap.Switch?? DefaultSwitch.True;
                if (@switch.CanContinue(context))
                {
                    this.RunAction(chainWrap.ActionChain, context);
                    if (group.SwitchKind == SwitchKind.Single)
                    {
                        break;
                    }
                }
            }
        }
        private void RunAction(ActionChain action,IActionContext context)
        {
            var res = ActionRunner.Default.RunAction(Entry, context);
            this.PublishOutput(res, context);
            if (res.IsSuccess)
            {
                this.RunActionGroup(Entry.OnSuccess, context);
            }
            else
            {
                this.RunActionGroup(Entry.OnErrors, context);
            }
            this.RunActionGroup(Entry.OnCompleted, context);
        }
        private void PublishOutput(ActionResult result, IActionContext context)
        {
            if (this.Entry.Outputs == null) return;
            foreach (var output in this.Entry.Outputs)
            {
                output.Output(result, context);
            }
        }
    }

    public interface IChainEntry
    {
        ActionChain Entry { get; set; }
    }


    public class Loop : IAction,INewContext, IChainEntry
    {
        public IEnumerable Source { get; set; }
        public ActionChain Entry { get; set; }



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

    public class For : IAction, INewContext
    {
        public int Start { get; set; }
        public int Count { get; set; }
        public int Step { get; set; }

        public string ItemName { get; set; }
        public ActionChain Entry { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            throw new NotImplementedException();
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
        IActionBuildService ActionBuildService { get; set; }
        IMetaDataService MetaDataService { get; set; }
        IActionTraceService TraceService { get; set; }
        ActionResult RunAction(IActionEntry actionEntry, IActionContext context);
    }
    public interface IActionTraceService
    {
        void TraceResult(IActionContext context,ActionResult result);
        
    }
    public class ActionRunner : IActionRunner
    {
        static ActionRunner()
        {
            Default = new ActionRunner();
        }
        public static IActionRunner Default { get; private set; }
        public IMetaDataService MetaDataService { get; set; }
        public IActionBuildService ActionBuildService { get; set; }

        public IActionTraceService TraceService { get; set; }
        public ActionResult RunAction(IActionEntry actionEntry, IActionContext context)
        {
            try
            {
                var meta = this.GetActionMeta(actionEntry);
                var action = this.CreateAction(meta);
                if (action is INewContext)
                {
                    using (var newcontext = context.BeginContext())
                    {
                        this.SetInputArguments(meta, action, actionEntry, newcontext);
                        var res= action.Exec(newcontext);
                        this.TraceService.TraceResult(newcontext,res);
                        return res;
                    }
                }
                else
                {
                    this.SetInputArguments(meta, action, actionEntry, context);
                    var res= action.Exec(context);
                    this.TraceService.TraceResult(context,res);
                    return res;
                }
            }
            catch (Exception ex)
            {
                var r= new ActionResult()
                {
                    Error = ex
                };
                this.TraceService.TraceResult(context,r);
                return r;
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
                var input = actionEntry.Inputs.SingleOrDefault(p => p.Name == inputMeta.Name);
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
