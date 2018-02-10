using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows.Meta
{
    public class ActionInputMeta
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public object DefaultValue { get; set; }

        public bool IsRequired { get; set; }

        public string Description { get; set; }
    }
}
