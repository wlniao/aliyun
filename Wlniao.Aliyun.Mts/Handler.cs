using System;
using System.Collections.Generic;
using System.Text;
using Wlniao.Handler;
namespace Wlniao.Aliyun.Mts
{
    /// <summary>
    /// 
    /// </summary>
    public class Handler : Wlniao.Aliyun.Handler
    {
        /// <summary>
        /// 
        /// </summary>
        public Handler()
        {
            PipelineHandler handler;
            handler = new ApiHandler();
            handler = new AliyunHandler(handler);
            handler = new MtsHandler(handler);
            handler = new RetryHandler(handler);
            inner = handler;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        public override void HandleBefore(IContext ctx)
        {
            inner.HandleBefore(ctx);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        public override void HandleAfter(IContext ctx)
        {
            inner.HandleAfter(ctx);
        }
    }
}