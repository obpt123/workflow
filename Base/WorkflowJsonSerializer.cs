using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Workflows.Meta;

namespace System.Workflows.Base
{
    public class WorkflowJsonSerializer : IActionSerializer
    {
        public const string ContentType = "workflow/json";

        private static Dictionary<string, Type> typeMaps = new Dictionary<string, Type>()
        {
            {"int",typeof(int) },
            {"string",typeof(string) },
            {"double",typeof(double) },
            {"float",typeof(float) },
            {"decimal",typeof(decimal) },
            {"datetime",typeof(DateTime) },
            {"timespan",typeof(TimeSpan) },
        };

        public ActionEntry DeSerialize(string content)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var workflowInfo = serializer.Deserialize<WorkflowInfo>(content);
            var parameters = from p in workflowInfo.parameters ?? new Dictionary<string, ParameterInfo>()
                             select new ActionInputMeta()
                             {
                                 Name = p.Key,
                                 Type = GetTypeByName(p.Value.type),
                                 IsRequired = p.Value.required,
                                 DefaultValue = p.Value.@default,
                                 Description = p.Value.description
                             };

            return new ActionEntry()
            {
                Meta = new ActionMeta()
                {
                    Ref = workflowInfo.name,
                    Kind = ActionKind.Workflow,
                    Description = workflowInfo.description,
                    DisplayFormat = workflowInfo.displayformat,
                    Parameters = parameters.ToList(),
                },
                Action = new WorkFlow()
                {
                    Body = BuildChain(workflowInfo.body),
                    Setup = BuildChain(workflowInfo.setup),
                    Teardown = BuildChain(workflowInfo.teardown),
                }
            };

        }


        private Type GetTypeByName(string typeName)
        {
            Type res;
            if (typeMaps.TryGetValue(typeName, out res))
            {
                return res;
            }
            return Type.GetType(typeName);

        }

        private ActionChain BuildChain(ChainInfo chainInfo)
        {
            if (chainInfo == null) return null;
            return BuildChain(chainInfo.entry, chainInfo.actions, new Dictionary<string, Workflows.ActionChain>());
        }
        private ActionChainGroup BuildGroup(TaskGroup group, Dictionary<string, ActionInfo> actions, Dictionary<string, ActionChain> maps)
        {
            if (group == null) return null;
            SwitchKind kind = group.kind == "single" ? SwitchKind.Single : SwitchKind.Mutile;
            var ac = from p in @group.tasks ?? Enumerable.Empty<Task>()
                     select new ActionChainWrapper()
                     {
                         ActionChain = BuildChain(p.name, actions, maps),
                         Switch = ActionUtility.ParseSwitch(p.@switch)
                     };

            return new ActionChainGroup()
            {
                SwitchKind = kind,
                Actions = ac.ToList()
            };
        }
        private ActionChain BuildChain(string entry, Dictionary<string, ActionInfo> actions, Dictionary<string, ActionChain> maps)
        {
            if (string.IsNullOrEmpty(entry)) return null;
            if (maps.ContainsKey(entry)) return maps[entry];
            if (!actions.ContainsKey(entry))
            {
                throw new ActionException($"不存在{entry}");
            }
            var info = actions[entry];
            var chain = new ActionChain()
            {
                ActionRef = info.type,
                Name = entry,
                Inputs = ActionUtility.ParseActionValues(info.input),
                Outputs = ActionUtility.ParseActionValues(info.output),
                OnCompleted = BuildGroup(info.oncompleted, actions, maps),
                OnErrors = BuildGroup(info.onerror, actions, maps),
                OnSuccess = BuildGroup(info.onsuccess, actions, maps),
                SubEntry = BuildChain(info.entry, info.actions, new Dictionary<string, ActionChain>()),
            };

            return chain;
        }
        public string Serialize(ActionEntry action)
        {
            throw new NotImplementedException();
        }
        #region InnerClass

        class WorkflowInfo
        {
            public string name { get; set; }
            public string description { get; set; }
            public string displayformat { get; set; }
            public Dictionary<string, ParameterInfo> parameters { get; set; }

            public ChainInfo setup { get; set; }

            public ChainInfo body { get; set; }

            public ChainInfo teardown { get; set; }
        }

        class ChainInfo
        {
            public string entry { get; set; }
            public Dictionary<string, ActionInfo> actions { get; set; }
        }

        class ParameterInfo
        {
            public string type { get; set; }
            public object @default { get; set; }
            public bool required { get; set; }
            public string description { get; set; }
        }

        class ActionInfo
        {
            public string type { get; set; }
            public Dictionary<string, object> input { get; set; }
            public Dictionary<string, object> output { get; set; }
            public TaskGroup onsuccess { get; set; }
            public TaskGroup onerror { get; set; }
            public TaskGroup oncompleted { get; set; }
            public string entry { get; set; }
            public Dictionary<string, ActionInfo> actions { get; set; }
        }

        class TaskGroup
        {
            public string kind { get; set; }

            public List<Task> tasks { get; set; }
        }
        class Task
        {
            public string @switch { get; set; }
            public string name { get; set; }
        }

        #endregion
    }
}
