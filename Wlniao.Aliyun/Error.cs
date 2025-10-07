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
        public string errcode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string errmsg { get; set; }
    }
}