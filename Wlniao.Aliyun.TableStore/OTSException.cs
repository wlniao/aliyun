/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Aliyun\TableStore\OTSException.cs
    适用环境：NETCoreCLR 1.0+
    功能描述：TableStore 异常

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

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// TableStore 异常
    /// </summary>
    public class OTSException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public OTSException()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public OTSException(string message) : base(message)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public OTSException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    /// <summary>
    /// TableStore 服务端异常
    /// </summary>
    public class OTSServerException : OTSException
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="httpStatusCode"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <param name="requestID"></param>
        public OTSServerException(string apiName, System.Net.HttpStatusCode httpStatusCode, string errorCode, string errorMessage, string requestID = null) : base(errorMessage ?? errorCode ?? apiName)
        {
            APIName = apiName;
            HttpStatusCode = httpStatusCode;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            RequestID = requestID;
        }
        /// <summary>
        /// 接口名称
        /// </summary>
        public string APIName { get; private set; }
        /// <summary>
        /// HTTP 状态码
        /// </summary>
        public System.Net.HttpStatusCode HttpStatusCode { get; private set; }
        /// <summary>
        /// 错误码
        /// </summary>
        public string ErrorCode { get; private set; }
        /// <summary>
        /// 错误描述
        /// </summary>
        public string ErrorMessage { get; private set; }
        /// <summary>
        /// 请求编号
        /// </summary>
        public string RequestID { get; private set; }
    }
}
