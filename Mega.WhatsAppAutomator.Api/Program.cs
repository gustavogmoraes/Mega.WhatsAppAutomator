using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Api.ApiUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mega.WhatsAppAutomator.Infrastructure;
using Microsoft.AspNetCore;
using Raven.Client.Extensions;

namespace Mega.WhatsAppAutomator.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            Console.WriteLine("Testing");
            // Creates automation
            //Task.Run(AutomationStartup.Start);
            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
             if (Convert.ToBoolean(Environment.GetEnvironmentVariable("IS_DEV_ENV")))
             {
                 return Host.CreateDefaultBuilder(args)
                     .ConfigureWebHostDefaults(webBuilder => webBuilder
                         .UseKestrel(opt => opt.Listen(IPAddress.Any, 80))
                         .UseStartup<Startup>());
             }
             
             return Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder => webBuilder
                        .UsePort()
                        .UseStartup<Startup>());
        }
    }
}
