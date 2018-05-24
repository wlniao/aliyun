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
            //var r = c.SubmitJobs("62a1f646a44d4060bb3d0260840e68ad", "6deae57592b845928758369313d024da", "daxianghai", "oss-cn-beijing", "asset/test.mp4");
            var r = c.SubmitMediaInfoJob("daxianghai", "oss-cn-beijing", "asset/test-zm.mp4");
            if (r.data.FirstIsH264())
            {

            }
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
