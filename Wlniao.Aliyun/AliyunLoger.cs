/*==============================================================================
    文件名称：AliyunLoger.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：基于Aliyun SLS服务的日志写入工具
================================================================================
 
    Copyright 2024 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/
using Aliyun.Api.LogService;
using Aliyun.Api.LogService.Domain.Log;
using Aliyun.Api.LogService.Domain.LogStore.Index;
using Aliyun.Api.LogService.Infrastructure.Serialization.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wlniao;
using Wlniao.Log;
namespace Wlniao.Aliyun
{
    /// <summary>
    /// 基于Aliyun SLS服务的日志写入工具
    /// </summary>
    public class AliyunLoger : ILogProvider
    {
        private static RegexOptions options = RegexOptions.None;
        private static Regex regexMsgId = new Regex(@"msgid:(.+),", options);
        private static Regex regexUseTime = new Regex(@"\[usetime:(.+)\]", options);
        private static Regex regexUrlLink = new Regex(@"((https?|ftp|file):\/\/[\-A-Za-z0-9+&@#\/%?=~_|!:,.;]*[\-A-Za-z0-9+&@#\/%=~_|])", options);
        private static readonly string[] levels = new string[] { "info", "warn", "debug", "error", "fatal" };
        /// <summary>
        /// 日志库名称
        /// </summary>
        public string StoreName { get; set; }
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string EndPortHost { get; set; }
        /// <summary>
        /// 日志项目名
        /// </summary>
        public string ProjectName { get; set; }
        /// <summary>
        /// AccessKeyId
        /// </summary>
        public string AccessKeyId { get; set; }
        /// <summary>
        /// AccessKeySecret
        /// </summary>
        public string AccessKeySecret { get; set; }
        /// <summary>
        /// 待写入数据流
        /// </summary>
        private static Queue<LogEntrie> queue = new Queue<LogEntrie>();
        /// <summary>
        /// 日志输出级别
        /// </summary>
        private LogLevel level = Loger.LogLevel;
        /// <summary>
        /// 
        /// </summary>
        public LogLevel Level
        {
            get
            {
                return level;
            }
        }
        /// <summary>
        /// 落盘时间间隔
        /// </summary>
        public int Interval = 0;

        private FileLoger flog = null;



        /// <summary>
        /// 
        /// </summary>
        public AliyunLoger()
        {
            NewAliyunLoger(LogLevel.None, null, null, null, null, null, 0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accesskey_id">AccessKeyId</param>
        /// <param name="accesskey_secret">AccessKeySecret</param>
        /// <param name="interval">落盘时间间隔（秒）</param>
        public AliyunLoger(string accesskey_id = null, string accesskey_secret = null, int interval = 0)
        {
            NewAliyunLoger(LogLevel.None, null, null, null, accesskey_id, accesskey_secret, interval);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level">日志输出级别</param>
        /// <param name="endport">服务器接入点</param>
        /// <param name="project">日志项目名称</param>
        /// <param name="store">日志存储库名称</param>
        /// <param name="interval">落盘时间间隔（秒）</param>
        public AliyunLoger(LogLevel level = LogLevel.None, string endport = null, string project = null, string store = null, int interval = 0)
        {
            NewAliyunLoger(level, endport, project, store, null, null, interval);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level">日志输出级别</param>
        /// <param name="endport">服务器接入点</param>
        /// <param name="project">日志项目名称</param>
        /// <param name="store">日志存储库名称</param>
        /// <param name="accesskey_id">AccessKeyId</param>
        /// <param name="accesskey_secret">AccessKeySecret</param>
        /// <param name="interval">落盘时间间隔（秒）</param>
        public AliyunLoger(LogLevel level = LogLevel.None, string endport = null, string project = null, string store = null, string accesskey_id = null, string accesskey_secret = null, int interval = 0)
        {
            NewAliyunLoger(level, endport, project, store, accesskey_id, accesskey_secret, interval);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level">日志输出级别</param>
        /// <param name="endport">服务器接入点</param>
        /// <param name="project">日志项目名称</param>
        /// <param name="store">日志存储库名称</param>
        /// <param name="accesskey_id">AccessKeyId</param>
        /// <param name="accesskey_secret">AccessKeySecret</param>
        /// <param name="interval">落盘时间间隔（秒）</param>
        private void NewAliyunLoger(LogLevel level = LogLevel.None, string endport = null, string project = null, string store = null, string accesskey_id = null, string accesskey_secret = null, int interval = 0)
        {
            this.level = level == LogLevel.None ? Loger.LogLevel : level;
            this.Interval = this.Interval > 0 ? this.Interval : cvt.ToInt(Config.GetConfigs("WLN_LOG_INTERVAL", "3"));
            this.AccessKeyId = string.IsNullOrEmpty(accesskey_id) ? Config.GetConfigs("WLN_LOG_KEYID", AccessKey.KeyId).TrimEnd('/') : accesskey_id;
            this.AccessKeySecret = string.IsNullOrEmpty(accesskey_secret) ? Config.GetConfigs("WLN_LOG_KEYSECRET", AccessKey.KeySecret).TrimEnd('/') : accesskey_secret;
            flog = new FileLoger(level);
            if (string.IsNullOrEmpty(endport))
            {
                if (this.EndPortHost == null)
                {
                    this.EndPortHost = Config.GetConfigs("WLN_LOG_SLS_ENDPORT", Config.GetConfigs("WLN_LOG_SERVER")).TrimEnd('/');
                }
                if (string.IsNullOrEmpty(this.EndPortHost))
                {
                    this.EndPortHost = "";
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(), "WLN_LOG_SLS_ENDPORT not configured, please set loki server."), ConsoleColor.Red);
                }
            }
            else
            {
                this.EndPortHost = endport.TrimEnd('/');
            }
            if (string.IsNullOrEmpty(project))
            {
                if (this.ProjectName == null)
                {
                    this.ProjectName = Config.GetConfigs("WLN_LOG_SLS_PROJECT").TrimEnd('/');
                }
                if (string.IsNullOrEmpty(this.ProjectName))
                {
                    this.ProjectName = "";
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(), "WLN_LOG_SLS_PROJECT not configured, please set loki server."), ConsoleColor.Red);
                }
            }
            else
            {
                this.ProjectName = project.Trim();
            }
            if (string.IsNullOrEmpty(store))
            {
                if (this.StoreName == null)
                {
                    this.StoreName = Config.GetConfigs("WLN_LOG_SLS_STORE").TrimEnd('/');
                }
                if (string.IsNullOrEmpty(this.StoreName))
                {
                    this.StoreName = "";
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(), "WLN_LOG_SLS_STORE not configured, please set loki server."), ConsoleColor.Red);
                }
            }
            else
            {
                this.StoreName = store.Trim();
            }
            if (this.Interval > 0 && !string.IsNullOrEmpty(EndPortHost) && !string.IsNullOrEmpty(ProjectName) && !string.IsNullOrEmpty(StoreName)
                && !string.IsNullOrEmpty(AccessKeyId) && !string.IsNullOrEmpty(AccessKeySecret))
            {
                Task.Run(() =>
                {
                    try
                    {
                        var client = LogServiceClientBuilders.HttpBuilder.RequestTimeout(2000) //设置每次请求超时时间
                             .Endpoint(EndPortHost, ProjectName) // 服务入口及项目名
                             .Credential(AccessKeyId, AccessKeySecret) // 访问密钥信息                                 
                             .Build();
                        var resQuery = client.GetLogStoreAsync(StoreName, ProjectName).Result;
                        if (!resQuery.IsSuccess && resQuery.Error != null && resQuery.Error.ErrorCode != null && resQuery.Error.ErrorCode.Code == "LogStoreNotExist")
                        {
                            var createStore = client.CreateLogStoreAsync(StoreName, 30, 1, ProjectName).Result;
                            if (createStore.IsSuccess)
                            {
                                var keys = new Dictionary<string, IndexKeyInfo>();
                                keys.TryAdd("level", new IndexKeyInfo("text") { });
                                keys.TryAdd("topic", new IndexKeyInfo("text") { });
                                keys.TryAdd("msgid", new IndexKeyInfo("text") { });
                                keys.TryAdd("urlto", new IndexKeyInfo("text") { });
                                client.CreateIndexAsync(StoreName, new IndexLineInfo(new char[0]), ProjectName).GetAwaiter();
                            }
                        }
                    }
                    catch { }
                });
                Task.Run(() =>
                {
                    while (true)
                    {
                        Write("", null, LogLevel.None, true);
                        Task.Delay(Interval * 1000).Wait();
                    }
                });
            }
        }
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="entrie"></param>
        /// <param name="push">是否立即回写</param>
        /// <param name="level">日志写入级别</param>
        private void Write(String topic, LogEntrie entrie, LogLevel level = LogLevel.None, bool push = false)
        {
            try
            {
                LogInfo item = null;
                if (entrie != null && !string.IsNullOrEmpty(topic) && !string.IsNullOrEmpty(entrie.content))
                {
                    if (levels.Contains(topic))
                    {
                        entrie.tags.TryAdd("level", topic);
                    }
                    else
                    {
                        entrie.tags.TryAdd("topic", topic);
                        if (level == LogLevel.Information)
                        {
                            entrie.tags.TryAdd("level", "info");
                        }
                        else if (level == LogLevel.Warning)
                        {
                            entrie.tags.TryAdd("level", "warn");
                        }
                        else if (level == LogLevel.Error)
                        {
                            entrie.tags.TryAdd("level", "error");
                        }
                        else if (level == LogLevel.Critical)
                        {
                            entrie.tags.TryAdd("level", "fatal");
                        }
                        else if (level == LogLevel.Debug)
                        {
                            entrie.tags.TryAdd("level", "debug");
                        }
                    }
                    //try
                    //{
                    //    var msgStart = entrie.content.IndexOf("msgid:");
                    //    var msgEnd = entrie.content.IndexOf(",", msgStart);
                    //    if (msgStart >= 0 && msgEnd > msgStart)
                    //    {
                    //        var msgtext = entrie.content.Substring(msgStart, msgEnd - msgStart + 1);
                    //        entrie.tags.TryAdd("msgid", msgtext.Substring(6, msgtext.Length - 7).Trim());
                    //        entrie.content = entrie.content.Replace(msgtext, "").Trim();
                    //    }
                    //}
                    //catch { }
                    try
                    {
                        foreach (Match m in regexMsgId.Matches(entrie.content))
                        {
                            entrie.content = entrie.content.Replace(m.Groups[0].ToString(), "").Trim();
                            entrie.tags.TryAdd("msgid", m.Groups[1].ToString());
                            break;
                        }
                    }
                    catch { }
                    try
                    {
                        foreach (Match m in regexUseTime.Matches(entrie.content))
                        {
                            entrie.content = entrie.content.Replace(m.Groups[0].ToString(), "").Trim();
                            entrie.tags.TryAdd("usetime", m.Groups[1].ToString());
                            break;
                        }
                    }
                    catch { }
                    try
                    {
                        foreach (Match m in regexUrlLink.Matches(entrie.content))
                        {
                            entrie.content = entrie.content.Replace(m.Groups[0].ToString(), "").Trim();
                            entrie.tags.TryAdd("urlto", m.Groups[1].ToString());
                            break;
                        }
                    }
                    catch { }
                    item = ConvertToDto(entrie);
                }
                if (push || this.Interval <= 0)
                {
                    var dto = new LogGroupInfo
                    {
                        Logs = new List<LogInfo>(),
                        Topic = "",
                        Source = XCore.WebNode,
                        LogTags = new Dictionary<string, string>()
                    };
                    if (item != null)
                    {
                        // 实时推送时，写入当前日志流
                        dto.Logs.Add(item);
                    }
                    lock (levels)
                    {
                        for (var i = 0; i < 20 && queue.Count > 0; i++)
                        {
                            // 同时写入队列中之前失败的日志流
                            var tmp = queue.Dequeue();
                            if (tmp != null)
                            {
                                dto.Logs.Add(ConvertToDto(tmp));
                            }
                        }
                    }
                    if (dto.Logs.Count > 0)
                    {
                        // 存在要推送的数据时，调用接口推送
                        var err = false;
                        try
                        {
                            var client = LogServiceClientBuilders.HttpBuilder.RequestTimeout(2000) //设置每次请求超时时间
                                 .Endpoint(EndPortHost, ProjectName) // 服务入口及项目名
                                 .Credential(AccessKeyId, AccessKeySecret) // 访问密钥信息                                 
                                 .Build();
                            var response = client.PostLogStoreLogsAsync(StoreName, dto).Result;
                            if (response == null || !response.IsSuccess)
                            {
                                err = true;
                                AliyunErrorLog("Push Result:" + response.Error.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            err = true;
                            AliyunErrorLog("Exception:" + ex.Message);
                        }
                        if (err)
                        {
                            lock (levels)
                            {
                                foreach (var log in dto.Logs)
                                {
                                    //失败时把待写入数据全部放入队列
                                    queue.Enqueue(new LogEntrie { tags = log.Contents, time = log.Time.DateTime, content = log.Contents["body"] });
                                }
                            }
                        }
                        else if (queue.Count > 20)
                        {
                            Task.Run(() =>
                            {
                                Write("", null, LogLevel.None, true);
                            });
                        }
                    }
                }
                else if (entrie != null && !string.IsNullOrEmpty(entrie.content))
                {
                    //定时落盘时把待写入数据放入日志流队列
                    queue.Enqueue(entrie);
                }
            }
            catch (Exception ex)
            {
                AliyunErrorLog("Exception:" + ex.Message);
            }
        }

        private string tmpmsg = null;
        /// <summary>
        /// Aliyun SLS异常时输出日志
        /// </summary>
        /// <param name="message"></param>
        private void AliyunErrorLog(string message)
        {
            if (message != tmpmsg)
            {
                tmpmsg = message;
                Loger.File("AliyunSLS", message, ConsoleColor.Red);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entrie"></param>
        /// <returns></returns>
        private LogInfo ConvertToDto(LogEntrie entrie)
        {            
            var dto = new LogInfo
            {
                Time = entrie.time,
                Contents = entrie.tags
            };
            if (dto.Contents.ContainsKey("body"))
            {
                dto.Contents["body"] = entrie.content;
            }
            else
            {
                dto.Contents.TryAdd("body", entrie.content);
            }
            return dto;
        }

        /// <summary>
        /// 输出Debug级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Debug(String message)
        {
            if (Level <= LogLevel.Debug)
            {
                var entrie = new LogEntrie { content = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.content), ConsoleColor.White);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("debug", message);
                }
                // Write("debug", entrie, LogLevel.None, false); //Debug级别日志不存储
            }
        }
        /// <summary>
        /// 输出Info级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Info(String message)
        {
            if (Level <= LogLevel.Information)
            {
                var entrie = new LogEntrie { content = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.content), ConsoleColor.Gray);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("info", message);
                }
                Write("info", entrie, LogLevel.None, false);
            }
        }

        /// <summary>
        /// 输出Warn级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Warn(String message)
        {
            if (Level <= LogLevel.Warning)
            {
                var entrie = new LogEntrie { content = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.content), ConsoleColor.DarkYellow);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("warn", message);
                }
                Write("warn", entrie, LogLevel.None, false);
            }
        }

        /// <summary>
        /// 输出Error级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Error(String message)
        {
            if (Level <= LogLevel.Error)
            {
                var entrie = new LogEntrie { content = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.content), ConsoleColor.Red);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("error", message);
                }
                Write("error", entrie, LogLevel.None, true);
            }
        }

        /// <summary>
        /// 输出Fatal级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Fatal(String message)
        {
            if (Level <= LogLevel.Critical)
            {
                var entrie = new LogEntrie { content = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.content), ConsoleColor.Magenta);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("fatal", message);
                }
                Write("fatal", entrie, LogLevel.None, true);
            }
        }

        /// <summary>
        /// 输出自定义主题的日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="logLevel"></param>
        /// <param name="consoleLocal"></param>
        public void Topic(String topic, String message, LogLevel logLevel, Boolean consoleLocal = true)
        {
            var entrie = new LogEntrie { content = message, time = DateTime.UtcNow };
            if (consoleLocal && Level <= logLevel)
            {
                if (Loger.LogLocal == "console")
                {
                    var color = ConsoleColor.DarkGray;
                    if (logLevel == LogLevel.Information)
                    {
                        color = ConsoleColor.Gray;
                    }
                    else if (logLevel == LogLevel.Debug)
                    {
                        color = ConsoleColor.White;
                    }
                    else if (logLevel == LogLevel.Error)
                    {
                        color = ConsoleColor.Red;
                    }
                    else if (logLevel == LogLevel.Warning)
                    {
                        color = ConsoleColor.DarkYellow;
                    }
                    else if (logLevel == LogLevel.Critical)
                    {
                        color = ConsoleColor.Magenta;
                    }
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.content), color);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write(topic, message);
                }
            }
            if (logLevel != LogLevel.Debug)
            {
                Write(topic, entrie, logLevel);
            }
        }
    }
}