using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class ActionMeta
    {
        public string Ref { get; set; }

        public ActionKind Kind { get; set; }

        public List<ActionInputMeta> Inputs { get; set; }

        public string DisplayFormat { get; set; }

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
    }
    public class ActionInfo
    {
        public ActionMeta Meta { get; set; }

        public IAction Action { get; set; }
    }

    public interface IActionFactoryService
    {
        ActionInfo GetAction(string actionRef);
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
        ActionInfo DeSerialize(string content);
        string Serialize(ActionInfo action);
    }

    public class ActionTypeNameSerializer : IActionSerializer
    {
        public const string ContentType = "action/typename";
        public ActionInfo DeSerialize(string content)
        {
            var type = Type.GetType(content);
            var action= Activator.CreateInstance(type) as IAction;
            return new ActionInfo()
            {
                Action = action
            };
        }

        public string Serialize(ActionInfo action)
        {
            return action.Action.GetType().AssemblyQualifiedName;
        }
    }

    public class WorkflowJsonSerializer : IActionSerializer
    {
        public const string ContentType = "workflow/json";

        public ActionInfo DeSerialize(string content)
        {
            throw new NotImplementedException();
        }

        public string Serialize(ActionInfo action)
        {
            throw new NotImplementedException();
        }
        #region InnerClass

        public class WorkflowInfo
        {
            public string name { get; set; }
            public string description { get; set; }
            public Dictionary<string, InputInfo> inputdefines { get; set; }
            public string entry { get; set; }
            public Dictionary<string,ActionInfo> actions { get; set; }
        }

        public class InputInfo
        {
            public string type { get; set; }
            public int @default { get; set; }
            public bool isrequired { get; set; }
            public string description { get; set; }
        }



        public class ActionInfo2
        {
            public string type { get; set; }
            public Dictionary<string,object> input { get; set; }
            public Dictionary<string, object> output { get; set; }
            public TaskGroup onsuccess { get; set; }
            public TaskGroup onerror { get; set; }
            public string entry { get; set; }
            public Dictionary<string, ActionInfo> actions { get; set; }
        }




        public class TaskGroup
        {
            public string Kind { get; set; }

            public List<Task> Tasks { get; set; }
        }
        public class Task
        {
            public string Switch { get; set; }
            public string Name { get; set; }
        }



















        #endregion
    }
}
