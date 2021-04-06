using System;
using System.Diagnostics;
using System.Net;
using Mega.WhatsAppAutomator.Api.ApiUtils;
using Mega.WhatsAppAutomator.Api.Filters;
using Mega.WhatsAppAutomator.Infrastructure;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mega.WhatsAppAutomator.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSession();
            services.AddControllersWithViews (options =>
            {
                options.Filters.Add (typeof (SessionFilter));
                options.Filters.Add (typeof (ExceptionHandlerFilter));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (!EnvironmentConfiguration.IsRunningOnHeroku)
            {
                app.UseDeveloperExceptionPage();
            }

            if (EnvironmentConfiguration.IsRunningOnHeroku)
            {
                app.UseHttpsRedirection();
            }
            
            app.UseRouting();

            app.UseCors(builder => builder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .Build());

            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            lifetime.ApplicationStopping.Register(OnAppStopping);
        }

        private void OnAppStopping()
        {
            if (!PupeteerMetadata.AmIRunningInDocker)
            {
                AutomationStartup.BrowserRef.CloseAsync().Wait();
            }
        }
    }
}
