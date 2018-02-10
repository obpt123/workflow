using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class ActionChain
    {
        public string Name { get; set; }
        public ActionChainGroup OnSuccess { get; set; }
        public ActionChainGroup OnErrors { get; set; }
        public ActionChainGroup OnCompleted { get; set; }
        public string ActionRef { get; set; }
        public List<IActionValueInfo> Inputs { get; set; }
        public List<IActionValueInfo> Outputs { get; set; }
        public ActionChain SubEntry { get; set; }
    }


}
