using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Workflows.Meta
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ActionInputAttribute : Attribute
    {
        public ActionInputAttribute(string name)
        {
            this.Name = name;
            this.IsRequired = false;
            
        }
        public string Name { get; set; }

        public string Description { get; set; }

        public object Default { get; set; }

        public bool IsRequired { get; set; }

        public static ActionInputMeta GetActionInputMeta(PropertyInfo propInfo)
        {
            var actionInputAttr = Attribute.GetCustomAttribute(propInfo, typeof(ActionInputAttribute)) as ActionInputAttribute;
            if (actionInputAttr == null) return null;
            return new ActionInputMeta()
            {
                Name = actionInputAttr.Name,
                DefaultValue = actionInputAttr.Name,
                Description = actionInputAttr.Description,
                IsRequired = actionInputAttr.IsRequired,
                Type = propInfo.PropertyType
            };

        }
    }
}
