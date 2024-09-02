using System;
using System.Collections.Generic;
namespace Wlniao.Aliyun.Mts.Request
{
    /// <summary>
    /// 提交转码作业的请求参数
    /// </summary>
    public class SubmitJobsRequest : Wlniao.Handler.IRequest
    {
        /// <summary>
        /// 管道ID，管道的定义详见术语表；若需要异步通知，须保证此管道绑定了可用的消息主题。
        /// </summary>
        public string PipelineId { get; set; }
        /// <summary>
        /// 输入文件所在OSS Bucket，需在控制台中资源控制频道里的Bucket授权页面授予此Bucket读权限给媒体处理服务，遵守OSS Bucket定义，见术语表Bucket
        /// </summary>
        public string InputBucket { get; set; }
        /// <summary>
        /// 输入OSS Bucket所在数据中心（OSS Location），遵守OSS Location定义，见术语表Location
        /// </summary>
        public string InputLocation { get; set; }
        /// <summary>
        /// 输入文件 （OSS Object），须进行UrlEncode，使用UTF-8编码，遵守OSS Object定义，见术语表Object
        /// </summary>
        public string InputObject { get; set; }
        /// <summary>
        /// 输出Bucket，需在控制台中完成云资源授权。
        /// </summary>
        public string OutputBucket { get; set; }
        /// <summary>
        /// 输出 Bucket 所在数据中心，默认值是oss-cn-hangzhou。
        /// </summary>
        public string OutputLocation { get; set; }
        /// <summary>
        /// 输出的文件名（OSS Object），须进行Url Encode，使用UTF-8编码。        占位符替换示例:转码输入文件若为a/b/c.flv,若OutputObject设置为%7BObjectPrefix%7D%7BFileName%7Dtest.mp4，那么转码输出文件名：a/b/ctest.mp4
        /// </summary>
        public string OutputObject { get; set; }
        /// <summary>
        /// 转码模板ID，支持自定义转码模板与系统预置模板。
        /// </summary>
        public string TemplateId { get; set; }
    }
}