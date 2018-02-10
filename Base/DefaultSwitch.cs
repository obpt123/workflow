using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class DefaultSwitch : ISwitch
    {
        public static ISwitch True = new DefaultSwitch(true);
        public static ISwitch False = new DefaultSwitch(false);

        public DefaultSwitch()
        {

        }
        public DefaultSwitch(bool val)
        {
            this.value = val;
        }
        private bool value;
        public bool CanContinue(IActionContext context)
        {
            return this.value;
        }
    }
}
