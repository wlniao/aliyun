using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wlniao.Aliyun
{
    /// <summary>
    /// 请求线程
    /// </summary>
    public class Context : Wlniao.Aliyun.IContext
    {
        /// <summary>
        /// AccessKeyId
        /// </summary>
        public string KeyId { get; set; }
        /// <summary>
        /// AccessKeySecret
        /// </summary>
        public string KeySecret { get; set; }
        /// <summary>
        /// HTTP请求方式
        /// </summary>
        public System.Net.Http.HttpMethod Method { get; set; }
        /// <summary>
        /// 要调用的API操作
        /// </summary>
        public string Operation { get; set; }
        /// <summary>
        /// 请求的地址
        /// </summary>
        public string RequestHost { get; set; }
        /// <summary>
        /// 要调用的路径
        /// </summary>
        public string RequestPath { get; set; }
        /// <summary>
        /// 要发送的请求内容
        /// </summary>
        public object Request { get; set; }
        /// <summary>
        /// API的输出内容
        /// </summary>
        public object Response { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Task<System.Net.Http.HttpResponseMessage> HttpTask;
        /// <summary>
        /// 请求使用的证书
        /// </summary>
        public System.Security.Cryptography.X509Certificates.X509Certificate Certificate;
        /// <summary>
        /// 输出的状态
        /// </summary>
        public System.Net.HttpStatusCode StatusCode = System.Net.HttpStatusCode.Created;
        /// <summary>
        /// 请求的Headers参数
        /// </summary>
        public Dictionary<string, string> HttpRequestHeaders;
        /// <summary>
        /// 输出的Headers参数
        /// </summary>
        public Dictionary<string, string> HttpResponseHeaders;
        /// <summary>
        /// 基础参数
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }
        /// <summary>
        /// 请求的内容
        /// </summary>
        public byte[] HttpRequestBody { get; set; }
        /// <summary>
        /// 请求的内容
        /// </summary>
        public string HttpRequestString { get; set; }
        /// <summary>
        /// 输出的内容
        /// </summary>
        public byte[] HttpResponseBody { get; set; }
        /// <summary>
        /// 输出的内容
        /// </summary>
        public string HttpResponseString { get; set; }
        /// <summary>
        /// 
        /// </summary>
        string IContext.Method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ApiPath { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object RequestBody { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public object ResponseBody { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Context()
        {
        }
    }
}