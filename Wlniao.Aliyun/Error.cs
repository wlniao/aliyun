using System;
namespace Wlniao.Aliyun
{
    /// <summary>
    /// 
    /// </summary>
    public class Error : Wlniao.Handler.IResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public String errcode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public String errmsg { get; set; }
    }
}