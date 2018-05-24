using System;
using System.Collections.Generic;
using System.Text;

namespace Wlniao.Aliyun.Mts.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class Input
    {
        /// <summary>
        /// 输入文件所在OSS Bucket
        /// </summary>
        public string Bucket { get; set; }
        /// <summary>
        /// 输入OSS Bucket所在数据中心（OSS Location）
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// 输入文件 （OSS Object）
        /// </summary>
        public string Object { get; set; }
    }
}
