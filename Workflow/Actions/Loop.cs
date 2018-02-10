using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class Loop : IAction, IChainEntry
    {
        public IEnumerable Source { get; set; }
        public ActionChain Entry { get; set; }
        public string ItemName { get; set; }
        public ActionResult Exec(IActionContext context)
        {
            List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();
            if (Source != null)
            {
                foreach (var item in Source)
                {
                    using (var newcontext = context.BeginContext())
                    {
                        if (!string.IsNullOrEmpty(ItemName))
                        {
                            newcontext.Vars[ItemName] = item;
                        }
                        ChainUtility.Run(this.Entry, newcontext);
                        res.Add(context.GetContextValues());
                    }
                }
            }
            return new ActionResult(res);
        }
    }
}
