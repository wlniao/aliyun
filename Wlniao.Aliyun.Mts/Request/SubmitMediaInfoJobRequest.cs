using System;
using System.Collections.Generic;
namespace Wlniao.Aliyun.Mts.Request
{
    /// <summary>
    /// 提交媒体信息作业的请求参数
    /// </summary>
    public class SubmitMediaInfoJobRequest : Wlniao.Handler.IRequest
    {
        /// <summary>
        /// 输入文件所在OSS Bucket
        /// </summary>
        public string InputBucket { get; set; }
        /// <summary>
        /// 输入OSS Bucket所在数据中心（OSS Location）
        /// </summary>
        public string InputLocation { get; set; }
        /// <summary>
        /// 输入文件 （OSS Object）
        /// </summary>
        public string InputObject { get; set; }
        /// <summary>
        /// 用户自定义数据，最大长度1024个字节。
        /// </summary>
        public string UserData { get; set; }
    }
}