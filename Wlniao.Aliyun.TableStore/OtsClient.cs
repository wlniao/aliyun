/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Aliyun\TableStore\OtsClient.cs
    适用环境：NETCoreCLR 1.0+
    功能描述：TableStore 原生 HTTP 客户端

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// TableStore 连接配置
    /// </summary>
    public class OTSClientConfig
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint">服务接入点</param>
        /// <param name="accessKeyID">AccessKeyId</param>
        /// <param name="accessKeySecret">AccessKeySecret</param>
        /// <param name="instanceName">实例名称</param>
        public OTSClientConfig(string endPoint, string accessKeyID, string accessKeySecret, string instanceName)
        {
            EndPoint = endPoint;
            AccessKeyID = accessKeyID;
            AccessKeySecret = accessKeySecret;
            InstanceName = instanceName;
        }
        /// <summary>
        /// 服务接入点
        /// </summary>
        public string EndPoint { get; set; }
        /// <summary>
        /// AccessKeyId
        /// </summary>
        public string AccessKeyID { get; set; }
        /// <summary>
        /// AccessKeySecret
        /// </summary>
        public string AccessKeySecret { get; set; }
        /// <summary>
        /// 实例名称
        /// </summary>
        public string InstanceName { get; set; }
        /// <summary>
        /// 协议版本
        /// </summary>
        public string APIVersion { get; set; } = "2015-12-31";
        /// <summary>
        /// 客户端标识
        /// </summary>
        public string UserAgent { get; set; } = "Wlniao.Aliyun.TableStore";
    }

    /// <summary>
    /// TableStore 原生 HTTP 客户端
    /// </summary>
    public class OTSClient
    {
        private static readonly HttpClient http = new HttpClient();
        private readonly OTSClientConfig config;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public OTSClient(OTSClientConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 获取表列表
        /// </summary>
        /// <param name="request"></param>
        public ListTableResponse ListTable(ListTableRequest request)
        {
            return Protocol.DecodeListTable(Send("/ListTable", new byte[0]));
        }

        /// <summary>
        /// 获取表结构
        /// </summary>
        /// <param name="request"></param>
        public DescribeTableResponse DescribeTable(DescribeTableRequest request)
        {
            return Protocol.DecodeDescribeTable(Send("/DescribeTable", Protocol.EncodeDescribeTable(request.TableName)));
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="request"></param>
        public CreateTableResponse CreateTable(CreateTableRequest request)
        {
            Send("/CreateTable", Protocol.EncodeCreateTable(request));
            return new CreateTableResponse();
        }

        /// <summary>
        /// 删除表
        /// </summary>
        /// <param name="request"></param>
        public DeleteTableResponse DeleteTable(DeleteTableRequest request)
        {
            Send("/DeleteTable", Protocol.EncodeDeleteTable(request.TableName));
            return new DeleteTableResponse();
        }

        /// <summary>
        /// 创建多元索引
        /// </summary>
        /// <param name="request"></param>
        public CreateSearchIndexResponse CreateSearchIndex(CreateSearchIndexRequest request)
        {
            Send("/CreateSearchIndex", Protocol.EncodeCreateSearchIndex(request));
            return new CreateSearchIndexResponse();
        }

        /// <summary>
        /// 写入行
        /// </summary>
        /// <param name="request"></param>
        public PutRowResponse PutRow(PutRowRequest request)
        {
            Send("/PutRow", Protocol.EncodePutRow(request));
            return new PutRowResponse();
        }

        /// <summary>
        /// 更新行
        /// </summary>
        /// <param name="request"></param>
        public UpdateRowResponse UpdateRow(UpdateRowRequest request)
        {
            Send("/UpdateRow", Protocol.EncodeUpdateRow(request));
            return new UpdateRowResponse();
        }

        /// <summary>
        /// 读取行
        /// </summary>
        /// <param name="request"></param>
        public GetRowResponse GetRow(GetRowRequest request)
        {
            return Protocol.DecodeGetRow(Send("/GetRow", Protocol.EncodeGetRow(request)));
        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="request"></param>
        public SearchResponse Search(SearchRequest request)
        {
            return Protocol.DecodeSearch(Send("/Search", Protocol.EncodeSearch(request)));
        }

        private byte[] Send(string apiName, byte[] body)
        {
            body = body ?? new byte[0];
            var headers = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["x-ots-contentmd5"] = System.Convert.ToBase64String(MD5.HashData(body)),
                ["x-ots-date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.000Z"),
                ["x-ots-apiversion"] = config.APIVersion,
                ["x-ots-accesskeyid"] = config.AccessKeyID,
                ["x-ots-instancename"] = config.InstanceName,
                ["x-ots-user-agent"] = config.UserAgent
            };
            headers["x-ots-signature"] = Sign(apiName, headers);

            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(config.EndPoint), apiName))
            {
                Content = new ByteArrayContent(body)
            };
            request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/x-protobuf");
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using (var response = http.Send(request))
            {
                var resBody = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    string code;
                    string message;
                    Protocol.DecodeError(resBody, out code, out message);
                    response.Headers.TryGetValues("x-ots-requestid", out var requestIds);
                    throw new OTSServerException(apiName, response.StatusCode, code, message, requestIds == null ? null : string.Join(",", requestIds));
                }
                return resBody;
            }
        }

        internal string Sign(string apiName, IDictionary<string, string> headers)
        {
            var headerString = string.Join("\n", FormatHeaders(headers));
            var signSource = apiName + "\nPOST\n\n" + headerString + "\n";
            using (var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(config.AccessKeySecret ?? string.Empty)))
            {
                return System.Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(signSource)));
            }
        }

        private static IEnumerable<string> FormatHeaders(IDictionary<string, string> headers)
        {
            var list = new List<string>();
            foreach (var header in headers)
            {
                if (header.Key.StartsWith("x-ots-", StringComparison.Ordinal))
                {
                    list.Add(header.Key + ":" + header.Value);
                }
            }
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }
}
