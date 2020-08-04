using System;
using System.Linq;
using System.Net;
using Mega.WhatsAppAutomator.Api.ApiUtils;
using Mega.WhatsAppAutomator.Domain.Objects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Mega.WhatsAppAutomator.Infrastructure;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.Utils;

namespace Mega.WhatsAppAutomator.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Sets running configs based on environment variables
            SetRunningConfiguration();
            
            var apiHost = CreateHostBuilder(args).Build();

            // Creates automation
            _ = AutomationStartup.Start();
            
            apiHost.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    if (EnvironmentConfiguration.IsRunningOnHeroku)
                    {
                        webBuilder.UsePort();
                        return;
                    }

                    webBuilder.UseKestrel(opt => opt.Listen(IPAddress.Any, EnvironmentConfiguration.LocalAspNetWebApiPort));
                });
        }

        public static void SetRunningConfiguration()
        {
            EnvironmentConfiguration.DatabaseName = Environment.GetEnvironmentVariable("DATABASE_NAME")
                ?? throw new Exception("ENV DATABASE_NAME not set");
            EnvironmentConfiguration.DatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? throw new Exception("ENV DATABASE_URL not set");
            EnvironmentConfiguration.DatabaseNeedsCertificate = (bool?)Convert.ToBoolean(Environment.GetEnvironmentVariable("DATABASE_NEEDS_CERT"))
                ?? throw new Exception("DATABASE_NEEDS_CERT not set");
            EnvironmentConfiguration.IsRunningOnHeroku = (bool?)Convert.ToBoolean(Environment.GetEnvironmentVariable("IS_RUNNING_ON_HEROKU"))
                ?? throw new Exception("ENV IS_RUNNING_ON_HEROKU not set");
            EnvironmentConfiguration.LocalAspNetWebApiPort = (int?)Convert.ToInt32(Environment.GetEnvironmentVariable("LOCAL_API_PORT"))
                ?? throw new Exception("LOCAL_API_PORT not set");
        }
    }
}
