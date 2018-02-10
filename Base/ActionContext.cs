using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    public class ActionContext : IActionContext
    {
        private Dictionary<Type, object> services = new Dictionary<Type, object>();
        public Dictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Vars { get; set; } = new Dictionary<string, object>();
        public IActionContext Parent { get; private set; }
        public List<IActionContext> SubContexts { get; private set; } = new List<IActionContext>();

        public int Depth { get; private set; } = 1;

        public IActionContext BeginContext()
        {
            ActionContext context = new ActionContext();
            context.Parent = this;
            context.Depth = this.Depth + 1;
            return context;
        }

        public bool ReleaseContext(IActionContext context)
        {
            return this.SubContexts.Remove(context);
        }

        void IDisposable.Dispose()
        {
            if (this.Parent != null)
            {
                this.Parent.ReleaseContext(this);
            }
        }

        public T GetService<T>()
        {
            object instance;
            if (this.services.TryGetValue(typeof(T), out instance))
            {
                return (T)instance;
            }
            else
            {
                if (this.Parent != null)
                {
                    return this.Parent.GetService<T>();
                }
                else
                {
                    return default(T);
                }
            }
        }

        public void RegistService<T>(T instance)
        {
            this.services[typeof(T)] = instance;
        }
        public Dictionary<string, object> GetContextValues()
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            IActionContext current = this;
            while (current != null)
            {
                MergeDic(current.Vars, res);
                MergeDic(current.Inputs, res);
                current = current.Parent;
            }
            return res;
        }
        private void MergeDic(Dictionary<string, object> from, Dictionary<string, object> to)
        {
            foreach (var kv in from)
            {
                if (!to.ContainsKey(kv.Key))
                {
                    to[kv.Key] = kv.Value;
                }
            }
        }
    }



}
