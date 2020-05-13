using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using CefSharp.OffScreen;

namespace Mega.WhatsAppAutomator.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var browser = new ChromiumWebBrowser("www.google.com");
            //Create a new instance in code
            // var browser = new ChromiumWebBrowser("www.google.com");

            //var settings = new CefSettings();
            //settings.BrowserSubprocessPath = @"x86\CefSharp.BrowserSubprocess.exe";

            // Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);


            var options = new ChromeOptions();
            var osArchitecture = System.Environment.Is64BitOperatingSystem ? "x64" : "x86";
            options.BinaryLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, osArchitecture, "libcef.dll");
            options.AddArguments("remote-debugging-port=12345");
            //options.AddArguments("headless", "disable-gpu", "no-sandbox", "disable-extensions"); // Headless
            options.AddArguments("--proxy-server='direct://'", "--proxy-bypass-list=*"); // Speed

            var driver = new ChromeDriver(options);
            driver.Navigate().GoToUrl("http://www.google.com/");

            var el = driver.FindElement(By.CssSelector("#tsf > div:nth-child(2) > div.A8SBwf > div.RNNXgb > div > div.a4bIc > input"));
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

                
    }
}
