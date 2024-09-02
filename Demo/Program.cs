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
            var c = Wlniao.strUtil.GetDomain("ssefs.ewer.wlniao.com.cn");
            var c1 = Wlniao.strUtil.GetDomainHost(c);
            var c2 = Wlniao.strUtil.GetDomainMain(c);
            var c3 = Wlniao.strUtil.GetDomainMainNoSuffix(c);
            if (c != c1 && c != c2 && c != c3)
            {

            }
            //var r = c.SubmitJobs("62a1f646a44d4060bb3d0260840e68ad", "6deae57592b845928758369313d024da", "daxianghai", "oss-cn-beijing", "asset/test.mp4");
            //var r = c.SubmitMediaInfoJob("daxianghai", "oss-cn-beijing", "asset/test-zm.mp4");
            //if (r.data.FirstIsH264())
            //{

            //}
            //log.Error(r.message);

            //var kvs = new List<KeyValuePair<String, String>>();
            //kvs.Add(new KeyValuePair<string, string>("Version", "2015-01-09"));
            //kvs.Add(new KeyValuePair<string, string>("Action", "AddDomainRecord"));
            //kvs.Add(new KeyValuePair<string, string>("DomainName", "wlniao.net"));
            //kvs.Add(new KeyValuePair<string, string>("RR", "ddns"));
            //kvs.Add(new KeyValuePair<string, string>("Type", "A"));
            //kvs.Add(new KeyValuePair<string, string>("Value", "127.0.0.1"));
            //kvs.Add(new KeyValuePair<string, string>("Line", "default"));
            //kvs.Add(new KeyValuePair<string, string>("Priority", "10"));
            //kvs.Add(new KeyValuePair<string, string>("TTL", "600"));
            //var test = c.PublicGet("https://alidns.aliyuncs.com", "/", kvs.ToArray());
            //if (test.success)
            //{

            //}
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
