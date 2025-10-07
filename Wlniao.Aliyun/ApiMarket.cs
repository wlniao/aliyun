using System;
using System.Collections.Generic;
using System.Linq;
using Wlniao.Text;
using Encoding = System.Text.Encoding;

namespace Wlniao.Aliyun
{
    /// <summary>
    /// 阿里云API市场请求
    /// </summary>
    public class ApiMarket
    {
        /// <summary>
        /// 发起Get请求
        /// </summary>
        /// <param name="ApiHost"></param>
        /// <param name="ApiPath"></param>
        /// <param name="AppKey"></param>
        /// <param name="AppSecret"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static ApiResult<string> GetString(string ApiHost, string ApiPath, string AppKey, string AppSecret, params KeyValuePair<string, string>[] kvs)
        {
            var rlt = new ApiResult<string>() { success = false, message = "", data = "" };
            if (string.IsNullOrEmpty(AppKey))
            {
                rlt.message = "云市场API应用AppKey未设置";
            }
            else if (string.IsNullOrEmpty(AppSecret))
            {
                rlt.message = "云市场API应用AppSecret未设置";
            }
            else
            {
                using var client = new System.Net.Http.HttpClient();
                try
                {
                    var time = DateTools.GetUnix();
                    var nonce = System.Guid.NewGuid().ToString();

                    var httpMethod = "GET";
                    var accept = "application/json";
                    var contentMD5 = "";
                    var contentType = "application/json; charset=utf-8";
                    var date = DateTools.ConvertToGMT();
                    var headers = new List<KeyValuePair<string, string>>();
                    headers.Add(new KeyValuePair<string, string>("X-Ca-Key", AppKey));
                    headers.Add(new KeyValuePair<string, string>("X-Ca-Nonce", nonce));
                    headers.Add(new KeyValuePair<string, string>("X-Ca-Timestamp", time.ToString() + "000"));
                    //headers.Add(new KeyValuePair<String, String>("X-Ca-Stage", "RELEASE"));
                    //headers.Add(new KeyValuePair<String, String>("X-Ca-Request-Mode", "debug"));
                    headers.Sort((KeyValuePair<string, string> a, KeyValuePair<string, string> b) => { return string.Compare(a.Key, b.Key, StringComparison.Ordinal); });
                    var headSign = "";
                    foreach (var header in headers)
                    {
                        headSign += header.Key + ":" + header.Value + "\n";
                    }
                    var url = "";
                    var urlSign = "";
                    var _kvs = new List<KeyValuePair<string, string>>(kvs);
                    _kvs.Sort((KeyValuePair<string, string> a, KeyValuePair<string, string> b) => { return string.Compare(a.Key, b.Key, StringComparison.Ordinal); });
                    foreach (var kv in _kvs)
                    {
                        url += kv.Key + "=" + kv.Value + "&";
                        if (string.IsNullOrEmpty(kv.Value))
                        {
                            urlSign += kv.Key + "&";
                        }
                        else
                        {
                            urlSign += kv.Key + "=" + kv.Value + "&";
                        }
                    }
                    url = (ApiHost + ApiPath + "?" + url).TrimEnd('&', '?');
                    urlSign = (ApiPath + "?" + urlSign).TrimEnd('&', '?');



                    var hmac = new System.Security.Cryptography.HMACSHA256() { Key = Encoding.UTF8.GetBytes(AppSecret.ToCharArray()) };
                    var signStr = httpMethod + "\n" + accept + "\n" + contentMD5 + "\n" + contentType + "\n" + date + "\n" + headSign + urlSign;
                    var sign = System.Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signStr.ToCharArray())));
                    #region
                    var uri = new Uri(url);
                    var hostSocket = Net.WlnSocket.GetSocket(uri.Host, uri.Port);
                    try
                    {
                        var reqStr = "";
                        reqStr += "GET " + uri.PathAndQuery + " HTTP/1.1";
                        reqStr += "\r\nHost: " + uri.Host;
                        reqStr += "\r\nDate: " + date;
                        reqStr += "\r\nAccept: " + accept;
                        reqStr += "\r\nContent-MD5: " + contentMD5;
                        reqStr += "\r\nContent-Type: " + contentType;
                        foreach (var header in headers)
                        {
                            reqStr += "\r\n" + header.Key + ": " + header.Value;
                        }
                        reqStr += "\r\nX-Ca-Signature: " + sign;
                        reqStr += "\r\nX-Ca-Signature-Headers: " + StringUtil.Join(",", headers.Select(a=>a.Key).ToArray());
                        reqStr += "\r\n";
                        reqStr += "\r\n";
                        var request = Encoding.UTF8.GetBytes(reqStr);
                        if (hostSocket.Send(request, request.Length, System.Net.Sockets.SocketFlags.None) > 0)
                        {
                            var str = "";
                            var length = 0;
                            var end = false;
                            var start = false;
                            var chunked = false;
                            while (true)
                            {
                                var rev = new byte[65535];
                                var index = hostSocket.Receive(rev, rev.Length, System.Net.Sockets.SocketFlags.None);
                                if (index == 0)
                                {
                                    break;
                                }
                                var tempstr = StringUtil.GetUTF8String(rev, 0, index);
                                var lines = tempstr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                                index = 0;
                                #region Headers处理
                                if (!start && lines[0].StartsWith("HTTP"))
                                {
                                    var ts = lines[0].Split(' ');
                                    if (ts[1] == "200")
                                    {
                                        for (index = 1; index < lines.Length; index++)
                                        {
                                            if (lines[index].ToLower().StartsWith("content-length"))
                                            {
                                                ts = lines[index].Split(' ');
                                                length = Wlniao.Convert.ToInt(ts[1]);
                                            }
                                            else if (lines[index].ToLower().StartsWith("transfer-encoding"))
                                            {
                                                chunked = lines[index].EndsWith("chunked");
                                            }
                                            if (string.IsNullOrEmpty(lines[index]))
                                            {
                                                index++;
                                                start = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (var line in lines)
                                        {
                                            if (line.SplitBy(":")[0] == "X-Ca-Error-Message")
                                            {
                                                rlt.message = line.Substring(line.IndexOf(' ') + 1);
                                                return rlt;
                                            }
                                        }
                                        rlt.message = lines.LastOrDefault();
                                        index = lines.Length;
                                        break;
                                    }
                                }
                                #endregion
                                #region 取文本内容
                                for (; index < lines.Length; index++)
                                {
                                    var line = lines[index];
                                    if (chunked)
                                    {
                                        index++;
                                        if (index < lines.Length)
                                        {
                                            var tempLength = Wlniao.Convert.DeHex(line, "0123456789abcdef");
                                            if (tempLength > 0)
                                            {
                                                length += (int)tempLength;
                                                line = lines[index];
                                            }
                                            else if (lines.Length == index + 2 && string.IsNullOrEmpty(lines[index + 1]))
                                            {
                                                end = true;
                                                break;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    if (index == 0 || (chunked && index == 1) || str.Length == 0)
                                    {
                                        str += line;
                                    }
                                    else
                                    {
                                        str += "\r\n" + line;
                                    }
                                    if (!chunked && System.Text.Encoding.UTF8.GetBytes(str).Length >= length)
                                    {
                                        end = true;
                                    }
                                }
                                if (end)
                                {
                                    break;
                                }
                                #endregion
                            }
                            if (!string.IsNullOrEmpty(str))
                            {
                                rlt.success = true;
                                rlt.message = "";
                                rlt.data = str;
                            }
                        }
                        hostSocket.Using = false;
                    }
                    catch (Exception e)
                    {
                        hostSocket.Using = false;
                        rlt.message = e.Message;
                    }
                    #endregion
                }
                catch { }
            }
            return rlt;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ApiHost"></param>
        /// <param name="ApiPath"></param>
        /// <param name="AppKey"></param>
        /// <param name="AppSecret"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static ApiResult<T> Get<T>(string ApiHost, string ApiPath, string AppKey, string AppSecret, params KeyValuePair<string, string>[] kvs)
        {
            var _rlt = GetString(ApiHost, ApiPath, AppKey, AppSecret, kvs);
            if (_rlt.success)
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<ApiResult<T>>(_rlt.data);
                }
                catch (Exception e)
                {
                    return new ApiResult<T>() { success = false, message = e.Message, data = default(T) };
                }
            }
            else
            {
                return new ApiResult<T>() { success = false, message = _rlt.message, data = default(T) };
            }
        }
    }
}