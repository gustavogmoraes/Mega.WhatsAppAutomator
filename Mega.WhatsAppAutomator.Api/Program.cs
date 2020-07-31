using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Api.ApiUtils;
using Mega.WhatsAppAutomator.Domain.Objects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mega.WhatsAppAutomator.Infrastructure;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using Microsoft.AspNetCore;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Extensions;

namespace Mega.WhatsAppAutomator.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var apiHost = CreateHostBuilder(args).Build();
            
            // Creates automation
            AutomationStartup.Start();
            
            apiHost.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
            {
                "Development" => Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder => webBuilder
                        .UseKestrel(opt => opt.Listen(IPAddress.Any, 5000))
                        .UseStartup<Startup>()),
                
                "Production_Local" => Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder => webBuilder
                        .UseKestrel(opt => opt.Listen(IPAddress.Any, 5000))
                        .UseStartup<Startup>()),
                
                "Production_Heroku" => Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder => webBuilder.UsePort().UseStartup<Startup>()),
                _ => null
            };
        }
    }
}
