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
    public interface IServiceProvider
    {
        T GetService<T>();
    }
    public class ActionContext : IActionContext

    {
        private Dictionary<Type, object> services = new Dictionary<Type, object>();
        public Dictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Vars { get; set; } = new Dictionary<string, object>();
        public IActionContext Parent { get; private set; }
        public List<IActionContext> SubContexts { get; private set; } = new List<IActionContext>();

        public int Depth { get; private set; } = 1;

        public IActionContext BeginContext()
        {
            ActionContext context = new ActionContext();
            context.Parent = this;
            context.Depth = this.Depth + 1;
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
            object instance;
            if (this.services.TryGetValue(typeof(T), out instance))
            {
                return (T)instance;
            }
            else
            {
                if (this.Parent != null)
                {
                    return this.Parent.GetService<T>();
                }
                else
                {
                    return default(T);
                }
            }
        }

        public void RegistService<T>(T instance)
        {
            this.services[typeof(T)] = instance;
        }
        public Dictionary<string, object> GetContextValues()
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            IActionContext current = this;
            while (current != null)
            {
                MergeDic(current.Vars, res);
                MergeDic(current.Inputs, res);
                current = current.Parent;
            }
            return res;
        }
        private void MergeDic(Dictionary<string, object> from, Dictionary<string, object> to)
        {
            foreach (var kv in from)
            {
                if (!to.ContainsKey(kv.Key))
                {
                    to[kv.Key] = kv.Value;
                }
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
            this.IsSuccess = true;
        }
        /// <summary>
        /// 执行过程的错误
        /// </summary>
        public Exception Error { get; set; }
        /// <summary>
        /// 执行结果
        /// </summary>
        public object Result { get; set; }

        public bool IsSuccess { get; set; }

        public static ActionResult FromContext(IActionContext context)
        {
            return new ActionResult(context.GetContextValues());
        }

    }
    /// <summary>
    /// 表示Action的接口
    /// </summary>
    public interface IAction
    {
        ActionResult Exec(IActionContext context);
    }
    public interface IActionValueInfo
    {
        string Name { get; set; }
        object GetValue(IActionContext context);
    }

    public class OriginalValueInfo : IActionValueInfo
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public object GetValue(IActionContext context)
        {
            return Value;
        }
    }
    public class TranslateValueInfo : IActionValueInfo
    {
        public string Name { get; set; }

        public string Expression { get; set; }

        public object GetValue(IActionContext context)
        {
            return null; ;
        }
    }




    public class ActionChain
    {
        public string Name { get; set; }
        public ActionChainGroup OnSuccess { get; set; }
        public ActionChainGroup OnErrors { get; set; }
        public ActionChainGroup OnCompleted { get; set; }
        public string ActionRef { get; set; }
        public List<IActionValueInfo> Inputs { get; set; }
        public List<IActionValueInfo> Outputs { get; set; }
        public ActionChain SubEntry { get; set; }
    }

    public class ChainUtility
    {
        public static void Run(ActionChain chain, IActionContext context)
        {
            var res = RunStep(chain, context);
            PublishOutput(chain, res, context);
            if (res.IsSuccess)
            {
                RunActionGroup(chain.OnSuccess, context, res);
            }
            else
            {
                RunActionGroup(chain.OnErrors, context, res);
            }
            RunActionGroup(chain.OnCompleted, context, res);
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
                var inputValues = ActionUtility.GetInputArguments(action, chain.Inputs, context);
                if (action.Meta.Kind == ActionKind.Workflow)
                {
                    using (var newcontext = context.BeginContext())
                    {
                        ActionUtility.SetInputArguments(action, inputValues, newcontext);
                        var res = action.Action.Exec(newcontext);
                        return res;
                    }
                }
                else
                {
                    ActionUtility.SetInputArguments(action, inputValues, context);
                    var res = action.Action.Exec(context);
                    return res;
                }


            }
            catch (Exception ex)
            {
                return new ActionResult()
                {
                    Error = ex,
                    IsSuccess = false,
                };
            }
        }
        private static void RunActionGroup(ActionChainGroup group, IActionContext context, ActionResult res)
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
        private static void PublishOutput(ActionChain chain, ActionResult result, IActionContext context)
        {

            PublishLastRes(context, result);
            foreach (var output in chain.Outputs ?? new List<IActionValueInfo>())
            {
                var val = output.GetValue(context);
                context.Vars[output.Name] = val;
            }
        }
        private static void PublishLastRes(IActionContext context, ActionResult res)
        {
            context.Vars["lastvalue"] = res.Result;
            context.Vars["lasterror"] = res.Error;
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
        public ActionChain Body { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            try
            {
                ChainUtility.Run(this.Setup, context);
                ChainUtility.Run(this.Body, context);
            }
            finally
            {
                ChainUtility.Run(this.Teardown, context);
            }
            return ActionResult.FromContext(context);
        }
    }

    public interface IChainEntry
    {
        ActionChain Entry { get; set; }
    }

    public class Group : IAction, IChainEntry
    {
        public ActionChain Entry { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            return null;
        }
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
                foreach (var item in Source)
                {
                    using (var newcontext = context.BeginContext())
                    {
                        if (!string.IsNullOrEmpty(ItemName))
                        {
                            newcontext.Vars[ItemName] = item;
                        }
                        ChainUtility.Run(this.Entry, newcontext);
                        res.Add(context.GetContextValues());
                    }
                }
            }
            return new ActionResult(res);
        }
    }

    public sealed class For : IAction, IChainEntry
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
                    ChainUtility.Run(this.Entry, newcontext);
                    res.Add(context.GetContextValues());
                }
            }

            return new ActionResult(res);
        }
    }

    public enum SwitchKind
    {
        /// <summary>
        /// 多条分支
        /// </summary>
        Mutile=0,
        /// <summary>
        /// 单条分支
        /// </summary>
        Single=1,
       
    }


    public interface IActionTraceService
    {
        void TraceResult(TraceContext context);

    }

    public class TraceContext
    {
        IActionContext ActionContext { get; set; }
        public int Depth { get; set; }

        public string ActionRef { get; set; }

        public string ActionName { get; set; }

        public Dictionary<string,object> InputValues { get; set; }

        public Dictionary<string,object> OutputValues { get; set; }

        public ActionResult Result { get; set; }
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
