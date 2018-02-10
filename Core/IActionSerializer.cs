using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public interface IActionSerializer
    {
        ActionEntry DeSerialize(string content);
        string Serialize(ActionEntry action);
    }
}
