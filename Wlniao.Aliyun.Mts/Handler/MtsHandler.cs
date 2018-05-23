using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Wlniao.Handler;
namespace Wlniao.Aliyun.Mts
{
    /// <summary>
    /// 
    /// </summary>
    public class MtsHandler : PipelineHandler
    {
        private Dictionary<string, ResponseEncoder> EncoderMap;
        private Dictionary<string, ResponseDecoder> DecoderMap;
        private delegate void ResponseEncoder(Context ctx);
        private delegate void ResponseDecoder(Context ctx);

        /// <summary>
        /// 
        /// </summary>
        public MtsHandler(PipelineHandler handler) : base(handler)
        {
            EncoderMap = new Dictionary<string, ResponseEncoder>() {
                { "SubmitJobs", SubmitJobsEncode },
            };
            DecoderMap = new Dictionary<string, ResponseDecoder>() {
                { "SubmitJobs", SubmitJobsDecode },
            };
        }

        #region Handle
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        public override void HandleBefore(IContext ctx)
        {
            var _ctx = (Context)ctx;
            EncoderMap[_ctx.Operation](_ctx);
            if (string.IsNullOrEmpty(_ctx.RequestPath))
            {
                _ctx.RequestPath = _ctx.Operation;
            }
            inner.HandleBefore(ctx);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        public override void HandleAfter(IContext ctx)
        {
            inner.HandleAfter(ctx);
            var _ctx = (Context)ctx;
            if (_ctx.HttpResponseString.Contains("errmsg") || _ctx.HttpResponseString.Contains("errcode"))
            {
                _ctx.Response = JsonConvert.DeserializeObject<Error>(_ctx.HttpResponseString);
            }
            else
            {
                DecoderMap[_ctx.Operation](_ctx);
            }
        }
        #endregion

        #region SubmitJobs
        private void SubmitJobsEncode(Context ctx)
        {
            var req = (Request.SubmitJobsRequest)ctx.Request;
            if (string.IsNullOrEmpty(req.OutputBucket))
            {
                req.OutputBucket = req.InputBucket;
            }
            if (string.IsNullOrEmpty(req.OutputLocation))
            {
                req.OutputLocation = req.InputLocation;
            }
            if (string.IsNullOrEmpty(req.OutputObject))
            {
                req.OutputObject = "%7BObjectPrefix%7D%7BFileName%7D-zm";
            }

            ctx.Method = System.Net.Http.HttpMethod.Get;
            ctx.Parameters.Add(new KeyValuePair<String, String>("Action", ctx.Operation));
            ctx.Parameters.Add(new KeyValuePair<String, String>("PipelineId", req.PipelineId));
            ctx.Parameters.Add(new KeyValuePair<String, String>("Input", Json.ToString(new { Bucket = req.InputBucket, Location = req.InputLocation, Object = req.InputObject })));
            ctx.Parameters.Add(new KeyValuePair<String, String>("OutputBucket", req.OutputBucket));
            ctx.Parameters.Add(new KeyValuePair<String, String>("OutputLocation", req.OutputLocation));
            ctx.Parameters.Add(new KeyValuePair<String, String>("Outputs", Newtonsoft.Json.JsonConvert.SerializeObject(new List<Object>(new[] { new { req.TemplateId, req.OutputObject } }))));


            ComputeSignature(ctx);

            #region 生成提交数据
            var sb = new System.Text.StringBuilder();
            foreach (var kv in ctx.Parameters)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("&");
                    }
                    sb.Append(kv.Key + "=" + strUtil.UrlEncode(kv.Value));
                }
            }
            ctx.HttpRequestString = sb.ToString();
            #endregion
        }
        private void SubmitJobsDecode(Context ctx)
        {
            if (ctx.HttpResponseBody.Length > 0)
            {
                ctx.Response = new Response.SubmitJobsResponse() { image = ctx.HttpResponseBody };
            }
            else
            {
                ctx.Response = new Error() { errmsg = "InvalidJsonString" };
            }
        }
        #endregion



        /// <summary>
        /// 计算签名
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private void ComputeSignature(Context ctx)
        {
            ctx.Parameters.Sort(delegate (KeyValuePair<String, String> small, KeyValuePair<String, String> big) { return small.Key.CompareTo(big.Key); });
            var values = new System.Text.StringBuilder();
            foreach (var kv in ctx.Parameters)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    if (values.Length > 0)
                    {
                        values.Append("&");
                    }
                    values.Append(kv.Key + "=" + strUtil.UrlEncode(kv.Value));
                }
            }
            var text = ctx.Method.ToString() + "&%2F&" + strUtil.UrlEncode(values.ToString());
            var hmac = new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.ASCII.GetBytes(ctx.KeySecret + "&"));
            var hashValue = hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes(text));
            var signature = System.Convert.ToBase64String(hashValue);
            ctx.Parameters.Add(new KeyValuePair<String, String>("Signature", signature));
        }
    }
}