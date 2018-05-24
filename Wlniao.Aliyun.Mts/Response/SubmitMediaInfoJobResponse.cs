using System;
using System.Collections.Generic;
namespace Wlniao.Aliyun.Mts.Response
{
    /// <summary>
    /// 提交媒体信息作业的输出内容
    /// </summary>
    public class SubmitMediaInfoJobResponse : Wlniao.Handler.IResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public Models.MediaInfoJob MediaInfoJob { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Boolean FirstIsH264()
        {
            if (MediaInfoJob != null && MediaInfoJob.State != null && MediaInfoJob.State.ToLower() == "success")
            {
                if (MediaInfoJob.Properties != null)
                {
                    var streams = MediaInfoJob.Properties.Streams;
                    if (streams != null && streams.VideoStreamList != null)
                    {
                        var videoStream = streams.VideoStreamList.VideoStream;
                        if (videoStream != null&&videoStream.Count>0)
                        {
                            var first = videoStream[0];
                            if (first.CodecName == "h264")
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}