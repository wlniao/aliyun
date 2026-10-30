using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Wlniao.Aliyun.TableStore.Test
{
    /// <summary>
    /// 客户端签名与请求收发测试。标准签名由阿里云官方 SDK 的 HttpHeaderHandler 生成。
    /// </summary>
    [TestClass]
    public class OtsClientTest
    {
        private const string SIGN_PUTROW = "nTGacKe3qXOYwLBbPtNbk5bbpeM=";

        [TestMethod]
        public void Sign_固定请求头_与标准签名一致()
        {
            var client = new OTSClient(new OTSClientConfig(
                "https://demo.cn-hangzhou.ots.aliyuncs.com", "akid-demo", "my-secret-key", "demo-instance"));
            var headers = new Dictionary<string, string>
            {
                ["x-ots-contentmd5"] = "1B2M2Y8AsgTpgAmY7PhCfg==",
                ["x-ots-date"] = "2026-01-01T00:00:00.000Z",
                ["x-ots-apiversion"] = "2015-12-31",
                ["x-ots-accesskeyid"] = "akid-demo",
                ["x-ots-instancename"] = "demo-instance",
                ["x-ots-user-agent"] = "Wlniao.Aliyun.TableStore",
            };

            Assert.AreEqual(SIGN_PUTROW, client.Sign("/PutRow", headers));
        }

        [TestMethod]
        public void ListTable_请求路径与请求头正确_响应被解析()
        {
            using var server = new TestServer(18471, 200, PlainBufferTest.HexToBytes(ProtocolTest.RES_LISTTABLE));
            var client = NewClient(18471);

            var res = client.ListTable(new ListTableRequest());
            var req = server.Captured;

            Assert.AreEqual("/ListTable", req.Path);
            Assert.AreEqual("POST", req.Method);
            Assert.AreEqual("akid-demo", req.GetHeader("x-ots-accesskeyid"));
            Assert.AreEqual("demo-instance", req.GetHeader("x-ots-instancename"));
            Assert.AreEqual("2015-12-31", req.GetHeader("x-ots-apiversion"));
            StringAssert.Matches(req.GetHeader("x-ots-date"), new System.Text.RegularExpressions.Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$"));
            Assert.AreEqual(System.Convert.ToBase64String(MD5.HashData(req.Body)), req.GetHeader("x-ots-contentmd5"));
            Assert.AreEqual(ComputeSignature("/ListTable", req, "my-secret-key"), req.GetHeader("x-ots-signature"));

            CollectionAssert.AreEqual(new[] { "t1", "t2" }, res.TableNames.ToArray());
        }

        [TestMethod]
        public void PutRow_请求体为编码后的protobuf()
        {
            using var server = new TestServer(18472, 200, new byte[0]);
            var client = NewClient(18472);
            var cols = new AttributeColumns
            {
                ["cnt"] = new ColumnValue(1L),
                ["name"] = new ColumnValue("abc"),
            };

            client.PutRow(new PutRowRequest("demo", new Condition(RowExistenceExpectation.EXPECT_NOT_EXIST),
                new PrimaryKey { ["id"] = new ColumnValue("r1") }, cols));
            var req = server.Captured;

            Assert.AreEqual("/PutRow", req.Path);
            PlainBufferTest.AssertHex(ProtocolTest.REQ_PUTROW, req.Body);
        }

        [TestMethod]
        public void UpdateRow_请求路径与请求体正确()
        {
            using var server = new TestServer(18475, 200, new byte[0]);
            var client = NewClient(18475);
            var update = new UpdateOfAttribute();
            update.AddAttributeColumnToDelete("oldcol");
            update.AddAttributeColumnToPut("cnt", new ColumnValue(7L));

            client.UpdateRow(new UpdateRowRequest("demo", new Condition(RowExistenceExpectation.EXPECT_EXIST),
                new PrimaryKey { ["id"] = new ColumnValue("r1") }, update));
            var req = server.Captured;

            Assert.AreEqual("/UpdateRow", req.Path);
            PlainBufferTest.AssertHex(ProtocolTest.REQ_UPDATEROW, req.Body);
        }

        [TestMethod]
        public void 错误响应_抛出OTSServerException并带上错误码与请求编号()
        {
            using var server = new TestServer(18473, 404, PlainBufferTest.HexToBytes(ProtocolTest.RES_ERROR),
                new Dictionary<string, string> { ["x-ots-requestid"] = "req-123" });
            var client = NewClient(18473);

            var ex = Assert.ThrowsExactly<OTSServerException>(() => client.ListTable(new ListTableRequest()));

            Assert.AreEqual("OTSObjectNotExist", ex.ErrorCode);
            Assert.AreEqual("The specified table does not exist.", ex.ErrorMessage);
            Assert.AreEqual("req-123", ex.RequestID);
            Assert.AreEqual("/ListTable", ex.APIName);
            Assert.AreEqual(HttpStatusCode.NotFound, ex.HttpStatusCode);
        }

        [TestMethod]
        public void GetRow_响应行被解析为主键与属性列()
        {
            using var server = new TestServer(18474, 200, PlainBufferTest.HexToBytes(ProtocolTest.RES_GETROW));
            var client = NewClient(18474);

            var res = client.GetRow(new GetRowRequest("demo", new PrimaryKey { ["id"] = new ColumnValue("r9") }));

            Assert.AreEqual("r9", res.PrimaryKey["id"].StringValue);
            Assert.AreEqual(5L, res.Attribute["cnt"].IntegerValue);
            Assert.AreEqual("hello", res.Attribute["name"].StringValue);
        }

        private static OTSClient NewClient(int port)
        {
            return new OTSClient(new OTSClientConfig(
                "http://127.0.0.1:" + port, "akid-demo", "my-secret-key", "demo-instance"));
        }

        /// <summary>
        /// 按协议约定重算请求签名，校验客户端签名的是其实际发送的请求头
        /// </summary>
        private static string ComputeSignature(string apiName, CapturedRequest req, string secret)
        {
            var parts = new List<string>();
            foreach (string key in req.Headers.Keys)
            {
                if (key.StartsWith("x-ots-", StringComparison.Ordinal) && key != "x-ots-signature")
                {
                    parts.Add(key + ":" + req.GetHeader(key));
                }
            }
            parts.Sort(StringComparer.Ordinal);
            var signSource = apiName + "\nPOST\n\n" + string.Join("\n", parts) + "\n";
            using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(secret));
            return System.Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(signSource)));
        }

        /// <summary>
        /// 捕获到的请求
        /// </summary>
        private sealed class CapturedRequest
        {
            public string Path = string.Empty;
            public string Method = string.Empty;
            public byte[] Body = Array.Empty<byte>();
            public Dictionary<string, string> Headers = new();

            public string GetHeader(string key)
            {
                foreach (var pair in Headers)
                {
                    if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return pair.Value;
                    }
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 处理一次请求的本地测试服务
        /// </summary>
        private sealed class TestServer : IDisposable
        {
            private readonly HttpListener listener = new();
            private readonly TaskCompletionSource<CapturedRequest> captured =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            /// <summary>
            /// 客户端请求完成后可读取到的请求内容
            /// </summary>
            public CapturedRequest Captured => captured.Task.GetAwaiter().GetResult();

            public TestServer(int port, int statusCode, byte[] body, Dictionary<string, string>? responseHeaders = null)
            {
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                listener.Start();
                _ = Task.Run(() =>
                {
                    var ctx = listener.GetContext();
                    var req = ctx.Request;
                    using var ms = new MemoryStream();
                    req.InputStream.CopyTo(ms);
                    var data = new CapturedRequest
                    {
                        Path = req.Url?.AbsolutePath ?? string.Empty,
                        Method = req.HttpMethod,
                        Body = ms.ToArray(),
                    };
                    foreach (string? key in req.Headers.AllKeys)
                    {
                        data.Headers[key ?? string.Empty] = req.Headers[key] ?? string.Empty;
                    }
                    captured.SetResult(data);

                    ctx.Response.StatusCode = statusCode;
                    if (responseHeaders != null)
                    {
                        foreach (var pair in responseHeaders)
                        {
                            ctx.Response.Headers[pair.Key] = pair.Value;
                        }
                    }
                    ctx.Response.ContentLength64 = body.Length;
                    ctx.Response.OutputStream.Write(body, 0, body.Length);
                    ctx.Response.Close();
                });
            }

            public void Dispose()
            {
                listener.Stop();
                listener.Close();
            }
        }
    }
}
