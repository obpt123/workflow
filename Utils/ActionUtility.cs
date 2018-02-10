using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Workflows.Meta;

namespace System.Workflows
{
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
                    return new ExpressionValueInfo()
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
}
