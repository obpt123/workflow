using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Workflows.Meta;

namespace System.Workflows
{
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

}
