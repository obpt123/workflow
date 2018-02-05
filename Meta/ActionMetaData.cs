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
    public class ActionEntry
    {
        public ActionMeta Meta { get; set; }

        public IAction Action { get; set; }
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
                Action = action
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
                    Parameters = parameters.ToList()

                },
                Action = new WorkFlow()
                {

                }  
            };

        }


        private Type GetTypeByName(string typeName)
        {
            return Type.GetType(typeName);

        }
        private List<IActionInput> GetInputs(Dictionary<string,object> inputs)
        {
            List<IActionInput> res = new List<IActionInput>();
            if (inputs != null)
            {
                foreach (var kv in inputs)
                {
                    if (kv.Value is string)
                    {
                        string str = kv.Value as string;
                        if (Regex.IsMatch(str, @"\${.+}"))
                        {
                            res.Add(new TranslateValueArgument()
                            {
                                Name = kv.Key,
                                Expression = str
                            });
                        }
                        else
                        {
                            res.Add(new OriginalValueArgument()
                            {
                                Name = kv.Key,
                                Value = str
                            });
                        }
                    }
                    else
                    {
                        res.Add(new OriginalValueArgument()
                        {
                             Name=kv.Key,
                             Value=kv.Value
                        });
                    }
                }
            }
            return res;
        }

        private List<IActionOutput> GetOutputs(Dictionary<string, object> outputs)
        {
            List<IActionOutput> res = new List<IActionOutput>();
            if (outputs != null)
            {
                foreach (var kv in outputs)
                {
                    if (kv.Value is string)
                    {
                        string str = kv.Value as string;
                        if (Regex.IsMatch(str, @"\${.+}"))
                        {
                            res.Add(new TranslateValueArgument()
                            {
                                Name = kv.Key,
                                Expression = str
                            });
                        }
                        else
                        {
                            res.Add(new OriginalValueArgument()
                            {
                                Name = kv.Key,
                                Value = str
                            });
                        }
                    }
                    else
                    {
                        res.Add(new OriginalValueArgument()
                        {
                            Name = kv.Key,
                            Value = kv.Value
                        });
                    }
                }
            }
            return res;
        }
        private ActionChain BuildChain(string entry, Dictionary<string, ActionInfo> actions)
        {
            if (!actions.ContainsKey(entry))
            {
                throw new ActionException($"不存在{entry}");
            }
            var info = actions[entry];
            var chain = new ActionChain()
            {
                ActionRef = info.type,
                Name = entry,
                 Inputs=GetInputs(info.input),
                  Outputs= GetOutputs(info.output),
                // Inputs=
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
