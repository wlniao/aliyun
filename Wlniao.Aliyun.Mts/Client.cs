using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wlniao;
using Wlniao.Aliyun.Mts.Request;
using Wlniao.Aliyun.Mts.Response;
using Wlniao.Log;

namespace Wlniao.Aliyun.Mts
{
    /// <summary>
    /// 阿里云API客户端
    /// </summary>
    public class Client : Wlniao.Aliyun.Client
    {
        #region 媒体服务配置信息
        internal static string _AliyunMtsRegionHost = null;
        internal static string _AliyunMtsAccessKeyId = null;
        internal static string _AliyunMtsAccessKeySecret = null;
        /// <summary>
        /// 服务地域 https://help.aliyun.com/document_detail/43248.html
        /// </summary>
        public static string AliyunMtsRegionHost
        {
            get
            {
                if (_AliyunMtsRegionHost == null)
                {
                    _AliyunMtsRegionHost = Config.GetSetting("AliyunMtsRegionHost");
                }
                return _AliyunMtsRegionHost;
            }
        }
        /// <summary>
        /// 阿里云颁发给用户的访问服务所用的密钥ID。
        /// </summary>
        public static string AliyunMtsAccessKeyId
        {
            get
            {
                if (_AliyunMtsAccessKeyId == null)
                {
                    _AliyunMtsAccessKeyId = Config.GetSetting("AliyunMtsAccessKeyId");
                    if (string.IsNullOrEmpty(_AliyunMtsAccessKeyId))
                    {
                        //未单独配置Mts服务的密钥时，使用统一配置的阿里云密钥
                        _AliyunMtsAccessKeyId = AccessKey.KeyId;
                    }
                }
                return _AliyunMtsAccessKeyId;
            }
        }
        /// <summary>
        /// 阿里云颁发给用户的访问服务所用的密钥。
        /// </summary>
        public static string AliyunMtsAccessKeySecret
        {
            get
            {
                if (_AliyunMtsAccessKeySecret == null)
                {
                    _AliyunMtsAccessKeySecret = Config.GetSetting("AliyunMtsAccessKeySecret");
                    if (string.IsNullOrEmpty(_AliyunMtsAccessKeySecret))
                    {
                        //未单独配置Mts服务的密钥时，使用统一配置的阿里云密钥
                        _AliyunMtsAccessKeySecret = AccessKey.KeySecret;
                    }
                }
                return _AliyunMtsAccessKeySecret;
            }
        }
        #endregion

        /// <summary>
        /// 基础参数
        /// </summary>
        public Dictionary<String, String> PublicParameters
        {
            get
            {
                var kvList = new Dictionary<String, String>();
                kvList.Add("Format", "JSON");
                kvList.Add("Version", "2015-01-09");
                kvList.Add("SignatureMethod", "HMAC-SHA1");
                kvList.Add("SignatureVersion", "1.0");
                kvList.Add("Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                return kvList;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Client()
        {
            this.KeyId = AliyunMtsAccessKeyId;
            this.KeySecret = AliyunMtsAccessKeySecret;
            this.RegionHost = AliyunMtsRegionHost;
            handler = new Handler();
        }
        /// <summary>
        /// 
        /// </summary>
        public Client(String KeyId, String KeySecret)
        {
            this.KeyId = KeyId;
            this.KeySecret = KeySecret;
            this.RegionHost = AliyunMtsRegionHost;
            handler = new Handler();
        }
        /// <summary>
        /// 
        /// </summary>
        public Client(String KeyId, String KeySecret, String RegionHost)
        {
            this.KeyId = KeyId;
            this.KeySecret = KeySecret;
            this.RegionHost = RegionHost;
            handler = new Handler();
        }

        /// <summary>
        /// 异步获取API接口输出
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="operation"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected Task<ApiResult<TResponse>> CallAsync<TRequest, TResponse>(string operation, TRequest request)
            where TResponse : Wlniao.Handler.IResponse, new()
            where TRequest : Wlniao.Handler.IRequest
        {
            if (request == null)
            {
                throw new ArgumentNullException();
            }
            else if (string.IsNullOrEmpty(RegionHost))
            {
                return Task<ApiResult<TResponse>>.Run(() =>
                {
                    return new ApiResult<TResponse>() { success = false, message = "AliyunMtsRegionHost not config" };
                });
            }
            else if (string.IsNullOrEmpty(KeyId))
            {
                return Task<ApiResult<TResponse>>.Run(() =>
                {
                    return new ApiResult<TResponse>() { success = false, message = "AliyunMtsAccessKeyId not config" };
                });
            }
            else if (string.IsNullOrEmpty(KeySecret))
            {
                return Task<ApiResult<TResponse>>.Run(() =>
                {
                    return new ApiResult<TResponse>() { success = false, message = "AliyunMtsAccessKeySecret not config" };
                });
            }

            var ctx = new Context();
            ctx.KeyId = KeyId;
            ctx.KeySecret = KeySecret;
            ctx.Parameters = PublicParameters;
            ctx.Method = System.Net.Http.HttpMethod.Get;
            ctx.RequestHost = RegionHost;
            ctx.RequestPath = "/";  //媒体服务请求路径默认为“/”
            ctx.Operation = operation;
            ctx.Request = request;
            ctx.Parameters.TryAdd("AccessKeyId", ctx.KeyId);
            ctx.Parameters.TryAdd("SignatureNonce", System.Guid.NewGuid().ToString());

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
        protected new TResponse GetResponseFromAsyncTask<TResponse>(Task<TResponse> task)
        {
            try
            {
                task.Wait();
            }
            catch (System.AggregateException e)
            {
                Loger.Error(e.Message);
                throw e.GetBaseException();
            }
            return task.Result;
        }


        #region SubmitJobs 提交转码作业
        /// <summary>
        /// 提交转码作业
        /// </summary>
        public ApiResult<SubmitJobsResponse> SubmitJobs(String PipelineId, String TemplateId, String Bucket, String Location, String InputObject, String OutputObject = null)
        {
            var request = new Wlniao.Aliyun.Mts.Request.SubmitJobsRequest()
            {
                PipelineId = PipelineId,
                TemplateId = TemplateId,
                InputBucket = Bucket,
                InputLocation = Location,
                InputObject = InputObject,
                OutputBucket = Bucket,
                OutputLocation = Location,
                OutputObject = OutputObject
            };
            return GetResponseFromAsyncTask(SubmitJobsAsync(request));
        }
        /// <summary>
        /// 提交转码作业
        /// </summary>
        public ApiResult<SubmitJobsResponse> SubmitJobs(SubmitJobsRequest request)
        {
            return GetResponseFromAsyncTask(SubmitJobsAsync(request));
        }
        /// <summary>
        /// 提交转码作业 的异步形式。
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<ApiResult<SubmitJobsResponse>> SubmitJobsAsync(SubmitJobsRequest request)
        {
            if (request == null)
            {
                return Task<ApiResult<SubmitJobsResponse>>.Run(() =>
                {
                    return new ApiResult<SubmitJobsResponse>() { message = "require parameters" };
                });
            }
            return CallAsync<SubmitJobsRequest, SubmitJobsResponse>("SubmitJobs", request);
        }
        #endregion

        #region SubmitMediaInfoJob 提交媒体信息作业
        /// <summary>
        /// 提交媒体信息作业
        /// </summary>
        public ApiResult<SubmitMediaInfoJobResponse> SubmitMediaInfoJob( String Bucket, String Location, String InputObject,String UserData = "")
        {
            var request = new SubmitMediaInfoJobRequest()
            {
                InputBucket = Bucket,
                InputLocation = Location,
                InputObject = InputObject,
                UserData = UserData
            };
            return GetResponseFromAsyncTask(SubmitMediaInfoJobAsync(request));
        }
        /// <summary>
        /// 提交媒体信息作业
        /// </summary>
        public ApiResult<SubmitMediaInfoJobResponse> SubmitMediaInfoJob(SubmitMediaInfoJobRequest request)
        {
            return GetResponseFromAsyncTask(SubmitMediaInfoJobAsync(request));
        }
        /// <summary>
        /// 提交媒体信息作业 的异步形式。
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<ApiResult<SubmitMediaInfoJobResponse>> SubmitMediaInfoJobAsync(SubmitMediaInfoJobRequest request)
        {
            if (request == null)
            {
                return Task<ApiResult<SubmitMediaInfoJobResponse>>.Run(() =>
                {
                    return new ApiResult<SubmitMediaInfoJobResponse>() { message = "require parameters" };
                });
            }
            return CallAsync<SubmitMediaInfoJobRequest, SubmitMediaInfoJobResponse>("SubmitMediaInfoJob", request);
        }
        #endregion
    }
}