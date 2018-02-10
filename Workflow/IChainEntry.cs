using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public interface IChainEntry
    {
        ActionChain Entry { get; set; }
    }
}
