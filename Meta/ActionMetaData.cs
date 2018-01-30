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

        public string ContentType { get; set; }

        public string Content { get; set; }
    }
    public class ActionContentTypes
    {
        public const string TypeName = "typename";
        public const string Xml = "xml";
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
    public interface IMetaDataService
    {
        ActionMeta GetMetaData(string actionRef);
    }
    public interface IActionBuildService
    {
        IAction BuildAction(string contentType, string content);
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
}
