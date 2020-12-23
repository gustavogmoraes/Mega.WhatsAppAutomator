using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Mega.WhatsAppAutomator.Api.ApiUtils;
using Mega.WhatsAppAutomator.Domain.Objects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Mega.WhatsAppAutomator.Infrastructure;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.WhatsAppAutomator.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //// Sets running configs based on environment variables for Docker or development IDE
            //// and based on args for dll running via "dotnet" command on CLI
            SetRunningConfiguration(args);
            
            var apiHost = CreateHostBuilder(args).Build();
            
            var appLifetime = apiHost.Services.GetRequiredService<IHostApplicationLifetime>();
            
            //// Exit application by graceful exit -> Watchtower
            appLifetime.ApplicationStopping.Register(AutomationStartup.ExitApplication);
            
            //// Creates automation
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

        private static void SetRunningConfiguration(IReadOnlyList<string> args)
        {
            if (args.Any())
            {
                EnvironmentConfiguration.DatabaseName = args[0] ?? throw new Exception("ENV DATABASE_NAME not set");
                EnvironmentConfiguration.DatabaseUrl = args[1] ?? throw new Exception("ENV DATABASE_URL not set");
                EnvironmentConfiguration.DatabaseNeedsCertificate = Convert.ToBoolean(args[2] ?? throw new Exception("DATABASE_NEEDS_CERTIFICATE not set"));
                EnvironmentConfiguration.IsRunningOnHeroku = Convert.ToBoolean(args[3] ?? throw new Exception("ENV IS_RUNNING_ON_HEROKU not set"));
                EnvironmentConfiguration.LocalAspNetWebApiPort = Convert.ToInt32(args[4] ?? throw new Exception("LOCAL_API_PORT not set"));
                EnvironmentConfiguration.UseHeadlessChromium = Convert.ToBoolean(args[5]);
                EnvironmentConfiguration.ClientId = args[6];
                EnvironmentConfiguration.InstanceId = args[7];
                
                return;
            }
            
            EnvironmentConfiguration.DatabaseName = Environment.GetEnvironmentVariable("DATABASE_NAME")
                                                    ?? throw new Exception("ENV DATABASE_NAME not set");
            EnvironmentConfiguration.DatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                                                   ?? throw new Exception("ENV DATABASE_URL not set");
            EnvironmentConfiguration.DatabaseNeedsCertificate = (bool?)Convert.ToBoolean(Environment.GetEnvironmentVariable("DATABASE_NEEDS_CERTIFICATE"))
                                                                ?? throw new Exception("DATABASE_NEEDS_CERT not set");
            EnvironmentConfiguration.IsRunningOnHeroku = (bool?)Convert.ToBoolean(Environment.GetEnvironmentVariable("IS_RUNNING_ON_HEROKU"))
                                                         ?? throw new Exception("ENV IS_RUNNING_ON_HEROKU not set");
            EnvironmentConfiguration.LocalAspNetWebApiPort = Convert.ToInt32(Environment.GetEnvironmentVariable("LOCAL_API_PORT"));
            EnvironmentConfiguration.UseHeadlessChromium = Convert.ToBoolean(Environment.GetEnvironmentVariable("USE_HEADLESS_CHROMIUM"));
            EnvironmentConfiguration.ClientId = Environment.GetEnvironmentVariable("CLIENT_ID")
                                                ?? throw new Exception("CLIENT_ID not set");
            EnvironmentConfiguration.InstanceId = Environment.GetEnvironmentVariable("INSTANCE_ID");
        }
    }
}
