using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Workflows.Meta;

namespace System.Workflows
{
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
            var factory = context.GetService<IActionFactory>();
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
}
