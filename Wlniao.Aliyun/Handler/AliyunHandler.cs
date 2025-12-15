using System;
using System.Collections.Generic;
using System.Text;
namespace Wlniao.Aliyun
{
    /// <summary>
    /// 
    /// </summary>
    public class AliyunHandler : PipelineHandler
    {
        /// <summary>
        /// 
        /// </summary>
        public AliyunHandler(PipelineHandler handler) : base(handler) { }

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