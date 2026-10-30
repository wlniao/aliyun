using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Wlniao.Aliyun
{
    /// <summary>
    /// 阿里云SLS（日志服务）原生HTTP客户端
    /// </summary>
    internal class SlsClient
    {
        private static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(2000) };
        private readonly Uri endpoint;
        private readonly string project;
        private readonly string accessKeyId;
        private readonly string accessKeySecret;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endportHost">服务接入点</param>
        /// <param name="project">日志项目名称</param>
        /// <param name="accessKeyId">AccessKeyId</param>
        /// <param name="accessKeySecret">AccessKeySecret</param>
        public SlsClient(string endportHost, string project, string accessKeyId, string accessKeySecret)
        {
            var host = endportHost ?? string.Empty;
            endpoint = host.Contains("://") ? new Uri(host) : new Uri("http://" + host);
            this.project = project;
            this.accessKeyId = accessKeyId;
            this.accessKeySecret = accessKeySecret;
        }

        /// <summary>
        /// 获取日志库信息
        /// </summary>
        /// <param name="logstoreName"></param>
        public Task<SlsResponse> GetLogStoreAsync(string logstoreName)
        {
            return SendAsync(HttpMethod.Get, "/logstores/" + logstoreName, null, null);
        }

        /// <summary>
        /// 创建日志库
        /// </summary>
        /// <param name="logstoreName"></param>
        /// <param name="ttl">数据保存时间（天）</param>
        /// <param name="shardCount">分片数</param>
        public Task<SlsResponse> CreateLogStoreAsync(string logstoreName, int ttl, int shardCount)
        {
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms))
            {
                writer.WriteStartObject();
                writer.WriteString("logstoreName", logstoreName);
                writer.WriteNumber("ttl", ttl);
                writer.WriteNumber("shardCount", shardCount);
                writer.WriteEndObject();
            }
            return SendAsync(HttpMethod.Post, "/logstores", ms.ToArray(), "application/json");
        }

        /// <summary>
        /// 创建日志库索引
        /// </summary>
        /// <param name="logstoreName"></param>
        public Task<SlsResponse> CreateIndexAsync(string logstoreName)
        {
            return SendAsync(HttpMethod.Post, "/logstores/" + logstoreName + "/index", Encoding.UTF8.GetBytes("{\"line\":{\"token\":[]}}"), "application/json");
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="logstoreName"></param>
        /// <param name="logGroup"></param>
        public Task<SlsResponse> PostLogStoreLogsAsync(string logstoreName, SlsLogGroup logGroup)
        {
            return SendAsync(HttpMethod.Post, "/logstores/" + logstoreName + "/shards/lb", EncodeLogGroup(logGroup), "application/x-protobuf");
        }

        private async Task<SlsResponse> SendAsync(HttpMethod method, string path, byte[] body, string contentType)
        {
            var date = DateTimeOffset.Now.ToString("r");
            var md5Hex = body == null ? "" : System.Convert.ToHexString(MD5.HashData(body));
            var canoHeaders = string.Join("\n", new[]
            {
                "x-log-apiversion:0.6.0",
                "x-log-bodyrawsize:" + (body == null ? 0 : body.Length),
                "x-log-signaturemethod:hmac-sha1"
            });
            var signSource = string.Join("\n", new[] { method.Method, md5Hex, contentType ?? "", date, canoHeaders, path });
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(accessKeySecret ?? string.Empty));
            var signature = System.Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signSource)));

            using var request = new HttpRequestMessage(method, new Uri(endpoint, path));
            request.Headers.TryAddWithoutValidation("Host", project + "." + endpoint.Host);
            request.Headers.TryAddWithoutValidation("Date", date);
            request.Headers.TryAddWithoutValidation("User-Agent", "Wlniao.Aliyun");
            request.Headers.TryAddWithoutValidation("x-log-apiversion", "0.6.0");
            request.Headers.TryAddWithoutValidation("x-log-bodyrawsize", body == null ? "0" : body.Length.ToString());
            request.Headers.TryAddWithoutValidation("x-log-signaturemethod", "hmac-sha1");
            request.Headers.TryAddWithoutValidation("Authorization", "LOG " + accessKeyId + ":" + signature);
            if (body != null)
            {
                request.Content = new ByteArrayContent(body);
                request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                request.Content.Headers.TryAddWithoutValidation("Content-MD5", md5Hex);
            }

            using var response = await http.SendAsync(request).ConfigureAwait(false);
            var rlt = new SlsResponse { IsSuccess = response.IsSuccessStatusCode };
            if (response.Headers.TryGetValues("x-log-requestid", out var requestIds))
            {
                rlt.RequestId = requestIds.FirstOrDefault();
            }
            if (!rlt.IsSuccess)
            {
                var res = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    using var doc = JsonDocument.Parse(res);
                    var root = doc.RootElement;
                    rlt.Error = new SlsError
                    {
                        Code = root.TryGetProperty("errorCode", out var code) ? code.GetString() : null,
                        Message = root.TryGetProperty("errorMessage", out var msg) ? msg.GetString() : null,
                    };
                }
                catch
                {
                    rlt.Error = new SlsError { Message = res };
                }
            }
            return rlt;
        }

        /// <summary>
        /// 按SLS protobuf协议编码日志组
        /// </summary>
        private static byte[] EncodeLogGroup(SlsLogGroup logGroup)
        {
            using var ms = new MemoryStream();
            if (logGroup.Logs != null)
            {
                foreach (var log in logGroup.Logs)
                {
                    using var ls = new MemoryStream();
                    var time = (uint)new DateTimeOffset(log.Time).ToUnixTimeSeconds();
                    if (time != 0)
                    {
                        WriteVarint(ls, 8);
                        WriteVarint(ls, time);
                    }
                    if (log.Contents != null)
                    {
                        foreach (var kv in log.Contents)
                        {
                            using var cs = new MemoryStream();
                            if (!string.IsNullOrEmpty(kv.Key))
                            {
                                WriteString(cs, 1, kv.Key);
                            }
                            WriteString(cs, 2, kv.Value ?? string.Empty);
                            WriteBytes(ls, 2, cs.ToArray());
                        }
                    }
                    WriteBytes(ms, 1, ls.ToArray());
                }
            }
            if (!string.IsNullOrEmpty(logGroup.Topic))
            {
                WriteString(ms, 3, logGroup.Topic);
            }
            if (!string.IsNullOrEmpty(logGroup.Source))
            {
                WriteString(ms, 4, logGroup.Source);
            }
            if (logGroup.LogTags != null)
            {
                foreach (var kv in logGroup.LogTags)
                {
                    using var ts = new MemoryStream();
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        WriteString(ts, 1, kv.Key);
                    }
                    WriteString(ts, 2, kv.Value ?? string.Empty);
                    WriteBytes(ms, 6, ts.ToArray());
                }
            }
            return ms.ToArray();
        }

        private static void WriteVarint(Stream stream, ulong value)
        {
            while (value >= 0x80)
            {
                stream.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }
            stream.WriteByte((byte)value);
        }

        private static void WriteBytes(Stream stream, int field, byte[] value)
        {
            WriteVarint(stream, (ulong)(field << 3 | 2));
            WriteVarint(stream, (ulong)value.Length);
            stream.Write(value, 0, value.Length);
        }

        private static void WriteString(Stream stream, int field, string value)
        {
            WriteBytes(stream, field, Encoding.UTF8.GetBytes(value));
        }
    }

    /// <summary>
    /// SLS接口响应
    /// </summary>
    internal class SlsResponse
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 请求编号
        /// </summary>
        public string RequestId { get; set; }
        /// <summary>
        /// 错误信息
        /// </summary>
        public SlsError Error { get; set; }
    }

    /// <summary>
    /// SLS接口错误信息
    /// </summary>
    internal class SlsError
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 错误描述
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// SLS日志组
    /// </summary>
    internal class SlsLogGroup
    {
        /// <summary>
        /// 日志主题
        /// </summary>
        public string Topic { get; set; }
        /// <summary>
        /// 日志来源
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// 日志标签
        /// </summary>
        public IDictionary<string, string> LogTags { get; set; }
        /// <summary>
        /// 日志内容
        /// </summary>
        public IList<SlsLog> Logs { get; set; }
    }

    /// <summary>
    /// SLS日志
    /// </summary>
    internal class SlsLog
    {
        /// <summary>
        /// 日志时间
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// 日志内容
        /// </summary>
        public IDictionary<string, string> Contents { get; set; }
    }
}
