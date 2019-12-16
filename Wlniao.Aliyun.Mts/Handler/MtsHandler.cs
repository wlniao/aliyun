using System;
using System.Linq;
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
                { "SubmitMediaInfoJob", SubmitMediaInfoJobEncode },
            };
            DecoderMap = new Dictionary<string, ResponseDecoder>() {
                { "SubmitJobs", SubmitJobsDecode },
                { "SubmitMediaInfoJob", SubmitMediaInfoJobDecode },
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
                req.OutputObject = "%7BObjectPrefix%7D%7BFileName%7D-zm{ExtName}";
            }

            ctx.Method = System.Net.Http.HttpMethod.Get;
            ctx.Parameters.TryAdd("Action", ctx.Operation);
            ctx.Parameters.TryAdd("PipelineId", req.PipelineId);
            ctx.Parameters.TryAdd("Input", Json.ToString(new { Bucket = req.InputBucket, Location = req.InputLocation, Object = req.InputObject }));
            ctx.Parameters.TryAdd("OutputBucket", req.OutputBucket);
            ctx.Parameters.TryAdd("OutputLocation", req.OutputLocation);
            ctx.Parameters.TryAdd("Outputs", JsonConvert.SerializeObject(new List<Object>(new[] { new { req.TemplateId, req.OutputObject } })));


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
            try
            {
                ctx.Response = Newtonsoft.Json.JsonConvert.DeserializeObject<Response.SubmitJobsResponse>(ctx.HttpResponseString);
            }
            catch
            {
                ctx.Response = new Error() { errmsg = "InvalidJsonString" };
            }
        }
        #endregion

        #region SubmitMediaInfoJob
        private void SubmitMediaInfoJobEncode(Context ctx)
        {
            var req = (Request.SubmitMediaInfoJobRequest)ctx.Request;

            ctx.Method = System.Net.Http.HttpMethod.Get;
            ctx.Parameters.TryAdd("Action", ctx.Operation);
            ctx.Parameters.TryAdd("Input", Json.ToString(new Models.Input { Bucket = req.InputBucket, Location = req.InputLocation, Object = req.InputObject }));
            ctx.Parameters.TryAdd("UserData", req.UserData);


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
        private void SubmitMediaInfoJobDecode(Context ctx)
        {
            try
            {
                ctx.Response = Newtonsoft.Json.JsonConvert.DeserializeObject<Response.SubmitMediaInfoJobResponse>(ctx.HttpResponseString);
            }
            catch
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
            var keys = ctx.Parameters.Keys.ToList();
            var values = new System.Text.StringBuilder();
            keys.Sort(delegate (String small, String big) { return string.Compare(small, big, StringComparison.Ordinal); });
            foreach (var key in keys)
            {
                if (!string.IsNullOrEmpty(ctx.Parameters[key]))
                {
                    if (values.Length > 0)
                    {
                        values.Append("&");
                    }
                    values.Append(key + "=" + strUtil.UrlEncode(ctx.Parameters[key]));
                }
            }
            var text = ctx.Method.ToString() + "&%2F&" + strUtil.UrlEncode(values.ToString());
            var hmac = new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.ASCII.GetBytes(ctx.KeySecret + "&"));
            var hashValue = hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes(text));
            var signature = System.Convert.ToBase64String(hashValue);
            ctx.Parameters.TryAdd("Signature", signature);
        }
    }
}