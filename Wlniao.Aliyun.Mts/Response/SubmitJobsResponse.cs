using System;
using System.Collections.Generic;
namespace Wlniao.Aliyun.Mts.Response
{
    /// <summary>
    /// 提交转码作业的输出内容
    /// </summary>
    public class SubmitJobsResponse : Wlniao.Handler.IResponse
    {
        /// <summary>
        /// 二维码数据
        /// </summary>
        public byte[] image { get; set; }
    }
}
