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
    public interface IActionContext:IDisposable , IServiceProvider
    {
        IActionContext Parent { get; }
        Dictionary<string, object> Inputs { get; set; }
        Dictionary<string,object> Vars { get; set; }
        Dictionary<string,object> Outputs { get; set; }

        List<IActionContext> SubContexts { get; }
        IActionContext BeginContext();

        bool ReleaseContext(IActionContext context);

        Dictionary<string, object> GetContextValues();
        
    }
    public interface IServiceProvider
    {
        T GetService<T>();
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

        public T GetService<T>()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> GetContextValues()
        {
            throw new NotImplementedException();
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


    public interface IActionInput
    {
        string Name { get; set; }
        object GetValue(IActionContext context);
    }

    public class OriginalValueArgument : IActionInput, IActionOutput
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public object GetValue(IActionContext context)
        {
            return Value;
        }
    }
    public class TranslateValueArgument : IActionInput, IActionOutput
    {
        public string Name { get; set; }

        public string Expression { get; set; }

        public object GetValue(IActionContext context)
        {
            return null; ;
        }
    }

    public interface IActionOutput
    {
        string Name { get; set; }


        object GetValue(IActionContext context);

    }


    public class ActionChain
    {
        public string Name { get; set; }
        public ActionChainGroup OnSuccess { get; set; }
        public ActionChainGroup OnErrors { get; set; }
        public ActionChainGroup OnCompleted { get; set; }

        public string ActionRef { get; set; }

        public List<IActionInput> Inputs { get; set; }

        public List<IActionOutput> Outputs { get; set; }

        public ActionChain SubEntry { get; set; }
    }

    public class ChainRunner
    {
        public static void Run(ActionChain chain, IActionContext context)
        {
            var res = RunStep(chain, context);
            //PublishOutput(res, context);
            if (res.IsSuccess)
            {
                RunActionGroup(chain.OnSuccess, context,res);
            }
            else
            {
                RunActionGroup(chain.OnErrors, context,res);
            }
            RunActionGroup(chain.OnCompleted, context,res);
        }
        private static ActionResult RunStep(ActionChain chain, IActionContext context)
        {
            var factory = context.GetService<IActionFactoryService>();
            var trace = context.GetService<IActionTraceService>();
            try
            {
                var action = factory.GetAction(chain.ActionRef);

                if (action.Action is IChainEntry)
                {
                    (action.Action as IChainEntry).Entry = chain.SubEntry;
                }
                var inputValues = GetInputArguments(action, chain, context);
                if (action.Meta.Kind == ActionKind.Workflow)
                {
                    using (var newcontext = context.BeginContext())
                    {
                        foreach (var kv in inputValues)
                        {
                            newcontext.Inputs[kv.Key] = kv.Value;
                        }
                        var res = action.Action.Exec(newcontext);
                        return res;
                    }
                }
                else
                {
                    var propMaps= action.Action.GetType().GetProperties().ToDictionary((p) =>
                    {
                        var att = Attribute.GetCustomAttribute(p, typeof(ActionInputAttribute)) as ActionInputAttribute;
                        return att == null ? p.Name : att.Name;

                    });
                    foreach (var val in inputValues)
                    {
                        if (propMaps.ContainsKey(val.Key))
                        {
                            propMaps[val.Key].SetValue(action.Action, val.Value, null);
                        }
                    }
                    var res = action.Action.Exec(context);
                    return res;
                }

               
            }
            catch (Exception ex)
            {
                var r = new ActionResult()
                {
                    Error = ex
                };
                return r;
            }
        }
        private static void RunActionGroup(ActionChainGroup group, IActionContext context,ActionResult res)
        {
            if (group == null || group.Actions == null) return;
            foreach (var chainWrap in group.Actions)
            {
                PublishLastRes(context, res);
                var @switch = chainWrap.Switch ?? DefaultSwitch.True;
                if (@switch.CanContinue(context))
                {
                    Run(chainWrap.ActionChain, context);
                    if (group.SwitchKind == SwitchKind.Single)
                    {
                        break;
                    }
                }
            }
        }



        private static Dictionary<string, object> GetInputArguments(ActionEntry action,ActionChain step, IActionContext context)
        {
            Dictionary<string, object> inputValues = new Dictionary<string, object>();
            foreach (var inputMeta in action.Meta.Parameters ?? new List<ActionInputMeta>())
            {
                var input = step.Inputs.SingleOrDefault(p => p.Name == inputMeta.Name);
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
                        inputValues.Add(inputMeta.Name, Convert.ChangeType(inputMeta.DefaultValue, inputMeta.Type));
                    }
                }
                else
                {
                    var value = input.GetValue(context);
                    inputValues.Add(inputMeta.Name, Convert.ChangeType(value, inputMeta.Type));
                }
            }
            return inputValues;
        }

        private void PublishOutput(ActionChain chain,ActionResult result, IActionContext context)
        {

            PublishLastRes(context, result);
            foreach (var output in chain.Outputs??new List<IActionOutput>())
            {
                var val = output.GetValue(context);
                context.Vars[output.Name] = val;
                //output.Output(result, context);
            }
        }
        private static void PublishLastRes(IActionContext context, ActionResult res)
        {
            context.Vars["last_val"] = res.Result;
            context.Vars["last_err"] = res.Error;
            context.Vars["last"] = res;
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


    public class WorkFlow : IAction
    {
        public ActionChain Setup { get; set; }

        public ActionChain Teardown { get; set; }
        public ActionChain Entry { get;  set; }


        public ActionResult Exec(IActionContext context)
        {
            try
            {
                ChainRunner.Run(this.Setup, context);
                ChainRunner.Run(this.Entry, context);
            }
            finally
            {
                ChainRunner.Run(this.Teardown, context);
            }
            return ActionResult.FromContext(context);
        }

    }

    public interface IChainEntry
    {
        ActionChain Entry { get; set; }
    }


    public class Loop : IAction, IChainEntry
    {
        public IEnumerable Source { get; set; }
        public ActionChain Entry { get; set; }
        public string ItemName { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();
            if (Source != null)
            {
                foreach(var item in Source)
                {
                    using (var newcontext = context.BeginContext())
                    {
                        if (!string.IsNullOrEmpty(ItemName))
                        {
                            newcontext.Vars[ItemName] = item;
                        }
                        ChainRunner.Run(this.Entry, newcontext);
                        res.Add(context.GetContextValues());
                    }
                }
            }
            return new ActionResult(res);
        }
    }

    public sealed class For : IAction
    {
        public int Start { get; set; }
        public int Count { get; set; }
        public int Step { get; set; }

        public string ItemName { get; set; }
        public ActionChain Entry { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();
            for (int i = 0, val = Start; i < Count; i++, val += Step)
            {
                using (var newcontext = context.BeginContext())
                {
                    if (!string.IsNullOrEmpty(ItemName))
                    {
                        newcontext.Vars[ItemName] = val;
                    }
                    ChainRunner.Run(this.Entry, newcontext);
                    res.Add(context.GetContextValues());
                }
            }

            return new ActionResult(res);
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


    public interface IActionTraceService
    {
        void TraceResult(IActionContext context,ActionResult result);
        
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
