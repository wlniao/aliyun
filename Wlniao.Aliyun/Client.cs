using System;
using System.Collections.Generic;
using System.Linq;
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
                log.Error(e.Message);
                throw e.GetBaseException();
            }
            return task.Result;
        }

    }
}