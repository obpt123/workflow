using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public sealed class For : IAction, IChainEntry
    {
        public int Start { get; set; }
        public int Count { get; set; }
        public int Step { get; set; }
        public string ItemName { get; set; }
        public ActionChain Entry { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();
            for (int i = 0, val = Start; i < Count; i++, val += Step)
            {
                using (var newcontext = context.BeginContext())
                {
                    if (!string.IsNullOrEmpty(ItemName))
                    {
                        newcontext.Vars[ItemName] = val;
                    }
                    ChainUtility.Run(this.Entry, newcontext);
                    res.Add(context.GetContextValues());
                }
            }

            return new ActionResult(res);
        }
    }
}
