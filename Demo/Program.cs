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
            var r = c.SubmitJobs(new Wlniao.Aliyun.Mts.Request.SubmitJobsRequest());
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
