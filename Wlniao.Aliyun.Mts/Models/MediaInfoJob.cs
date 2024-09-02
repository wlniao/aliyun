using System;
using System.Collections.Generic;
using System.Text;

namespace Wlniao.Aliyun.Mts.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class MediaInfoJob
    {
        /// <summary>
        /// 
        /// </summary>
        public string JobId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Input Input { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string UserData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Properties Properties { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string PipelineId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string CreationTime { get; set; }
    }
}
