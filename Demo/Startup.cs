using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.StaticFiles;
namespace Demo
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute("Path", "{controller}/{action}", new { action = "Index" });
                routes.MapRoute("Home", "{action}", new { controller = "Web" });
                routes.MapRoute("Root", "", new { controller = "Web", action = "Home" });
            });
        }
    }
}
