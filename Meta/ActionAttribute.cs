using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows.Meta
{
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
            if (actionAttr == null) return null;
            var inputs = from p in type.GetProperties()
                         let v= ActionInputAttribute.GetActionInputMeta(p)
                         where v!=null
                         select v;
                         
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
}
