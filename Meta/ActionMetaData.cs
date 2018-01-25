using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class ActionMetaData
    {
        public string ActionName { get; set; }

        public ActionKind Kind { get; set; }

        public ArgumentMetaInfoCollection Arguments { get; set; }
    }
    public enum ActionKind
    {
        Workflow,
        Action
    }

    public class ArgumentMetaInfo
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public object DefaultValue { get; set; }

        public bool IsRequired { get; set; }
    }

    public class ArgumentMetaInfoCollection:List<ArgumentMetaInfo>
    {

    }

    public interface IMetaDataService
    {
        ActionMetaData GetMetaData(string actionName);
    }
}
