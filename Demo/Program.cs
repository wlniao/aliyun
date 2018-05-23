using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Wlniao;

namespace Demo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var c = new Wlniao.Aliyun.Mts.Client();
            var r = c.SubmitJobs(new Wlniao.Aliyun.Mts.Request.SubmitJobsRequest()
            {
                PipelineId = "62a1f646a44d4060bb3d0260840e68ad",
                TemplateId = "6deae57592b845928758369313d024da",
                InputBucket = "daxianghai",
                InputLocation = "oss-cn-beijing",
                InputObject= "201805/0521/1j96wsa4vz.mp4"
            });
            log.Error(r.message);
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls("http://*:5000")
                .Build();
            host.Run();            
        }
    }
}
