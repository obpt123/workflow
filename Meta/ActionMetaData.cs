using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace System.Workflows
{
    public class ActionMeta
    {
        public string Ref { get; set; }

        public ActionKind Kind { get; set; }

        public List<ActionInputMeta> Parameters { get; set; }

        public string DisplayFormat { get; set; }

        public string Description { get; set; }

    }

    public enum ActionKind
    {
        Workflow,
        Action
    }

    public class ActionInputMeta
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public object DefaultValue { get; set; }

        public bool IsRequired { get; set; }

        public string Description { get; set; }
    }
    public sealed class ActionEntry
    {
        public ActionMeta Meta { get; set; }

        public IAction Action { get; set; }
    }

    public static class ActionUtility
    {
        public static ISwitch ParseSwitch(string exp)
        {
            if (string.IsNullOrEmpty(exp)) return null;
            return null;
        }
        public static IActionValueInfo ParseActionValues(string name, object value)
        {
            if (value is string)
            {
                string str = value as string;
                var match = Regex.Match(str, @"\${(?<exp>.+)}", RegexOptions.Singleline);
                if (match.Success)
                {
                    return new TranslateValueInfo()
                    {
                        Name = name,
                        Expression = match.Groups["exp"].Value
                    };
                }
                else
                {
                    return new OriginalValueInfo()
                    {
                        Name = name,
                        Value = str
                    };
                }
            }
            else
            {
                return new OriginalValueInfo()
                {
                    Name = name,
                    Value = value
                };
            }
        }
        public static List<IActionValueInfo> ParseActionValues(Dictionary<string, object> inputs)
        {
            if (inputs == null) return null;
            return inputs.Select(p => ParseActionValues(p.Key, p.Value)).ToList();
        }
        public static Dictionary<string, object> GetInputArguments(ActionEntry action, List<IActionValueInfo> inputs, IActionContext context)
        {
            Dictionary<string, object> inputValues = new Dictionary<string, object>();
            foreach (var inputMeta in action.Meta.Parameters ?? new List<ActionInputMeta>())
            {
                var input = (inputs ?? new List<IActionValueInfo>()).SingleOrDefault(p => p.Name == inputMeta.Name);
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

        public static void SetInputArguments(ActionEntry action, Dictionary<string, object> values, IActionContext context)
        {
            if (values == null || values.Count == 0) return;
            if (action.Meta.Kind == ActionKind.Action)
            {
                var propMaps = action.Action.GetType().GetProperties().ToDictionary((p) =>
                {
                    var att = Attribute.GetCustomAttribute(p, typeof(ActionInputAttribute)) as ActionInputAttribute;
                    return att == null ? p.Name : att.Name;

                });
                foreach (var val in values)
                {
                    if (propMaps.ContainsKey(val.Key))
                    {
                        propMaps[val.Key].SetValue(action.Action, val.Value, null);
                    }
                }
            }
            else
            {
                foreach (var kv in values)
                {
                    context.Inputs[kv.Key] = kv.Value;
                }
            }
        }


    }
    public static class ActionTask
    {
        public static ActionResult Run(ActionEntry entry, List<IActionValueInfo> inputs, IActionContext context)
        {
            var arguments = ActionUtility.GetInputArguments(entry, inputs, context);
            ActionUtility.SetInputArguments(entry, arguments, context);
            return entry.Action.Exec(context);

        }
    }

    public interface IActionFactoryService
    {
        ActionEntry GetAction(string actionRef);
    }


    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ActionAttribute : Attribute
    {

        readonly string refName;

        public ActionAttribute(string refName)
        {
            this.refName = refName;
        }

        public string RefName
        {
            get { return this.refName; }
        }
        public string Description { get; set; }

        public string DisplayFormat { get; set; }

        public static ActionMeta GetActionMeta(Type type)
        {
            ActionAttribute actionAttr = Attribute.GetCustomAttribute(type, typeof(ActionAttribute)) as ActionAttribute;
            var inputs = from p in type.GetProperties()
                         let actionInputAttr = Attribute.GetCustomAttribute(type, typeof(ActionInputAttribute)) as ActionInputAttribute
                         where actionInputAttr != null
                         select new ActionInputMeta()
                         {
                             Name = actionInputAttr.Name,
                             DefaultValue = actionInputAttr.Name,
                             Description = actionInputAttr.Description,
                             IsRequired = actionInputAttr.IsRequired,
                             Type = p.PropertyType
                         };
            return new ActionMeta()
            {
                Ref = actionAttr.RefName,
                Description = actionAttr.Description,
                DisplayFormat = actionAttr.DisplayFormat,
                Kind = ActionKind.Action,
                Parameters = inputs.ToList()
            };

        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ActionInputAttribute : Attribute
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public object Default { get; set; }

        public bool IsRequired { get; set; }
    }

    public interface IActionSerializer
    {
        ActionEntry DeSerialize(string content);
        string Serialize(ActionEntry action);
    }

    public class ActionTypeNameSerializer : IActionSerializer
    {
        public const string ContentType = "action/typename";
        public ActionEntry DeSerialize(string content)
        {
            var type = Type.GetType(content);
            var action = Activator.CreateInstance(type) as IAction;
            return new ActionEntry()
            {
                Action = action,
                Meta = ActionAttribute.GetActionMeta(type)
            };
        }

        public string Serialize(ActionEntry action)
        {
            return action.Action.GetType().AssemblyQualifiedName;
        }
    }

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
