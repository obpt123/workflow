using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Workflows
{
    /// <summary>
    /// 表示Action的执行结果
    /// </summary>
    public class ActionResult
    {
        public ActionResult()
        {

        }
        public ActionResult(object result) : this(result, true)
        {
        }
        public ActionResult(object result, bool success)
        {
            this.Result = result;
            this.IsSuccess = success;
        }
        /// <summary>
        /// 执行过程的错误
        /// </summary>
        public Exception Error { get; set; }
        /// <summary>
        /// 执行结果
        /// </summary>
        public object Result { get; set; }

        public bool IsSuccess { get; set; }

        public static ActionResult FromException(Exception exception)
        {
            return new ActionResult()
            {
                Error = exception,
                IsSuccess = false,
            };
        }
        public static ActionResult FromContext(IActionContext context)
        {
            return new ActionResult(context.GetContextValues());
        }

    }


}
