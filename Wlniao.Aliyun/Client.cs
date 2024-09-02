using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wlniao.Aliyun
{
    /// <summary>
    /// 阿里云API客户端
    /// </summary>
    public class Client : Wlniao.Handler.IClient
    {
        /// <summary>
        /// 阿里云AccessKeyId
        /// </summary>
        public string KeyId { get; set; }
        /// <summary>
        /// 阿里云AccessKeySecret
        /// </summary>
        public string KeySecret { get; set; }
        /// <summary>
        /// 服务地域服务器
        /// </summary>
        public string RegionHost { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Handler handler = null;
        /// <summary>
        /// 
        /// </summary>
        public Client()
        {
            this.KeyId = AccessKey.KeyId;
            this.KeySecret = AccessKey.KeySecret;
            handler = new Handler();
        }
        /// <summary>
        /// 
        /// </summary>
        public Client(String KeyId, String KeySecret)
        {
            this.KeyId = KeyId;
            this.KeySecret = KeySecret;
            handler = new Handler();
        }

        /// <summary>
        /// 异步获取API接口输出
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="operation"></param>
        /// <param name="request"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        protected Task<ApiResult<TResponse>> CallAsync<TRequest, TResponse>(string operation, TRequest request, System.Net.Http.HttpMethod method)
            where TResponse : Wlniao.Handler.IResponse, new()
            where TRequest : Wlniao.Handler.IRequest
        {
            if (request == null)
            {
                throw new ArgumentNullException();
            }

            var ctx = new Context();
            ctx.KeyId = KeyId;
            ctx.KeySecret = KeySecret;
            ctx.Method = method == null ? System.Net.Http.HttpMethod.Get : method;
            ctx.Operation = operation;
            ctx.Request = request;

            handler.HandleBefore(ctx);
            if (ctx.Response == null)
            {
                return ctx.HttpTask.ContinueWith((t) =>
                {
                    handler.HandleAfter(ctx);
                    if (ctx.Response is Error)
                    {
                        var err = (Error)ctx.Response;
                        return new ApiResult<TResponse>() { success = false, message = err.errmsg, code = err.errcode };
                    }
                    return new ApiResult<TResponse>() { success = true, message = "success", data = (TResponse)ctx.Response };
                });
            }
            else
            {
                if (ctx.Response is Error)
                {
                    var err = (Error)ctx.Response;
                    return Task<ApiResult<TResponse>>.Run(() =>
                    {
                        return new ApiResult<TResponse>() { success = false, message = err.errmsg, code = err.errcode };
                    });
                }
                else
                {
                    return Task<ApiResult<TResponse>>.Run(() =>
                    {
                        return new ApiResult<TResponse>() { success = true, message = "error", data = (TResponse)ctx.Response };
                    });
                }
            }
        }
        /// <summary>
        /// 同步获取API接口输出
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        protected TResponse GetResponseFromAsyncTask<TResponse>(Task<TResponse> task)
        {
            try
            {
                task.Wait();
            }
            catch (System.AggregateException e)
            {
                Log.Loger.Error(e.Message);
                throw e.GetBaseException();
            }
            return task.Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ApiHost"></param>
        /// <param name="ApiPath"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public String PublicGet(String ApiHost, String ApiPath, params KeyValuePair<String, String>[] kvs)
        {
            if (string.IsNullOrEmpty(this.KeyId))
            {
                return "阿里云AccessKeyId未设置";
            }
            else if (string.IsNullOrEmpty(this.KeySecret))
            {
                return "阿里云AliyunKeySecret未设置";
            }
            else
            {
                var ctx = new Context();
                ctx.KeyId = KeyId;
                ctx.KeySecret = KeySecret;
                ctx.RequestHost = ApiHost;
                ctx.RequestPath = ApiPath;
                ctx.Method = System.Net.Http.HttpMethod.Get;
                ctx.Parameters = new Dictionary<String, String>();
                if (kvs != null)
                {
                    foreach (var kv in kvs)
                    {
                        ctx.Parameters.Add(kv.Key, kv.Value);
                    }
                }

                ctx.Parameters.TryAdd("Format", "JSON");
                ctx.Parameters.TryAdd("AccessKeyId", ctx.KeyId);
                ctx.Parameters.TryAdd("Timestamp", DateTools.FormatUtc("yyyy-MM-dd'T'HH:mm:ss'Z'"));
                ctx.Parameters.TryAdd("SignatureMethod", "HMAC-SHA1");
                ctx.Parameters.TryAdd("SignatureVersion", "1.0");
                ctx.Parameters.TryAdd("SignatureNonce", Guid.NewGuid().ToString());
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

                try
                {
                    handler.HandleBefore(ctx);
                    handler.HandleAfter(ctx);
                }
                catch { }
                return ctx.HttpResponseString;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ApiHost"></param>
        /// <param name="ApiPath"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public String PublicPost(String ApiHost, String ApiPath, params KeyValuePair<String, String>[] kvs)
        {
            if (string.IsNullOrEmpty(this.KeyId))
            {
                return "阿里云AccessKeyId未设置";
            }
            else if (string.IsNullOrEmpty(this.KeySecret))
            {
                return "阿里云AliyunKeySecret未设置";
            }
            else
            {
                var ctx = new Context();
                ctx.KeyId = KeyId;
                ctx.KeySecret = KeySecret;
                ctx.RequestHost = ApiHost;
                ctx.RequestPath = ApiPath;
                ctx.Method = System.Net.Http.HttpMethod.Post;
                ctx.Parameters = new Dictionary<String, String>();
                if (kvs != null)
                {
                    foreach (var kv in kvs)
                    {
                        ctx.Parameters.Add(kv.Key, kv.Value);
                    }
                }

                ctx.Parameters.TryAdd("Format", "JSON");
                ctx.Parameters.TryAdd("AccessKeyId", ctx.KeyId);
                ctx.Parameters.TryAdd("Timestamp", DateTools.FormatUtc("yyyy-MM-dd'T'HH:mm:ss'Z'"));
                ctx.Parameters.TryAdd("SignatureMethod", "HMAC-SHA1");
                ctx.Parameters.TryAdd("SignatureVersion", "1.0");
                ctx.Parameters.TryAdd("SignatureNonce", Guid.NewGuid().ToString());
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

                try
                {
                    handler.HandleBefore(ctx);
                    handler.HandleAfter(ctx);
                }
                catch { }
                return ctx.HttpResponseString;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ApiHost"></param>
        /// <param name="ApiPath"></param>
        /// <param name="Content"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public String PublicPost(String ApiHost, String ApiPath, String Content, params KeyValuePair<String, String>[] kvs)
        {
            if (string.IsNullOrEmpty(this.KeyId))
            {
                return "阿里云AccessKeyId未设置";
            }
            else if (string.IsNullOrEmpty(this.KeySecret))
            {
                return "阿里云AliyunKeySecret未设置";
            }
            else
            {
                var ctx = new Context();
                ctx.KeyId = KeyId;
                ctx.KeySecret = KeySecret;
                ctx.RequestHost = ApiHost;
                ctx.RequestPath = ApiPath;
                ctx.Method = System.Net.Http.HttpMethod.Post;
                ctx.Parameters = new Dictionary<String, String>();
                if (kvs != null)
                {
                    foreach (var kv in kvs)
                    {
                        ctx.Parameters.Add(kv.Key, kv.Value);
                    }
                }

                ctx.Parameters.TryAdd("Format", "JSON");
                ctx.Parameters.TryAdd("AccessKeyId", ctx.KeyId);
                ctx.Parameters.TryAdd("Timestamp", DateTools.FormatUtc("yyyy-MM-dd'T'HH:mm:ss'Z'"));
                ctx.Parameters.TryAdd("SignatureMethod", "HMAC-SHA1");
                ctx.Parameters.TryAdd("SignatureVersion", "1.0");
                ctx.Parameters.TryAdd("SignatureNonce", Guid.NewGuid().ToString());
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
                if (ctx.RequestPath.IndexOf('?') > 0)
                {
                    ctx.RequestPath += "&" + sb.ToString();
                }
                else
                {
                    ctx.RequestPath += "?" + sb.ToString();
                }
                ctx.HttpRequestString = Content;
                #endregion

                try
                {
                    handler.HandleBefore(ctx);
                    handler.HandleAfter(ctx);
                }
                catch { }
                return ctx.HttpResponseString;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ApiHost"></param>
        /// <param name="ApiPath"></param>
        /// <param name="Content"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public String PublicPost(String ApiHost, String ApiPath, byte[] Content, params KeyValuePair<String, String>[] kvs)
        {
            if (string.IsNullOrEmpty(this.KeyId))
            {
                return "阿里云AccessKeyId未设置";
            }
            else if (string.IsNullOrEmpty(this.KeySecret))
            {
                return "阿里云AliyunKeySecret未设置";
            }
            else
            {
                var ctx = new Context();
                ctx.KeyId = KeyId;
                ctx.KeySecret = KeySecret;
                ctx.RequestHost = ApiHost;
                ctx.RequestPath = ApiPath;
                ctx.Method = System.Net.Http.HttpMethod.Post;
                ctx.Parameters = new Dictionary<String, String>();
                if (kvs != null)
                {
                    foreach (var kv in kvs)
                    {
                        ctx.Parameters.Add(kv.Key, kv.Value);
                    }
                }

                ctx.Parameters.TryAdd("Format", "JSON");
                ctx.Parameters.TryAdd("AccessKeyId", ctx.KeyId);
                ctx.Parameters.TryAdd("Timestamp", DateTools.FormatUtc("yyyy-MM-dd'T'HH:mm:ss'Z'"));
                ctx.Parameters.TryAdd("SignatureMethod", "HMAC-SHA1");
                ctx.Parameters.TryAdd("SignatureVersion", "1.0");
                ctx.Parameters.TryAdd("SignatureNonce", Guid.NewGuid().ToString());
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
                if (ctx.RequestPath.IndexOf('?') > 0)
                {
                    ctx.RequestPath += "&" + sb.ToString();
                }
                else
                {
                    ctx.RequestPath += "?" + sb.ToString();
                }
                ctx.HttpRequestBody = Content;
                #endregion

                try
                {
                    handler.HandleBefore(ctx);
                    handler.HandleAfter(ctx);
                }
                catch { }
                return ctx.HttpResponseString;
            }
        }


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
            ctx.Parameters.Add("Signature", signature);
        }
    }
}