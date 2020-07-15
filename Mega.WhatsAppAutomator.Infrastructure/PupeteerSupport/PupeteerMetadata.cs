using System.Linq;
using System.Runtime.InteropServices;
using System;
using System.IO;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using PuppeteerSharp;
using Extensions = PuppeteerSharp.Extensions;

namespace Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport
{
    public class PupeteerMetadata
    {
        public static string CustomUserAgentForHeadless => @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3641.0 Safari/537.36";
        public static string[] CustomsArgsForHeadless => new string[] { "--disable-gpu", "--no-sandbox", "--disable-extensions", "--proxy-server='direct://'", "--proxy-bypass-list=*" };
        private static string UserDataDir => Path.Combine(PupeteerBasePath, "user-data-dir");
        public static BrowserFetcherOptions FetcherOptions => new BrowserFetcherOptions { Path = FetcherDownloadPath };
        private static string FetcherDownloadPath => Path.Combine(PupeteerBasePath, "Browser");
        private static string PupeteerBasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PupeteerFiles");
        private static string ExecutablePath => SolveExecutablePath();
        public static bool AmIRunningInDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        public static LaunchOptions GetLaunchOptions(bool headless)
        {
            var options = new LaunchOptions
            {
                Headless = headless,
                UserDataDir = UserDataDir,
                ExecutablePath = ExecutablePath
            };

            if (!AmIRunningInDocker)
            {
                return options;
            }
            
            // These arguments are mandatory if running on container.
            options.Args = options.Args.ArrayAdd("no-sandbox");
            options.Headless = true;
            
            return options;
        }

        private static string SolveExecutablePath()
        {
            if (AmIRunningInDocker)
            {
                Console.WriteLine($"Running in docker, returning path as {Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH")}");
                return Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
            }
            var osPlatform = DevOpsHelper.GetOsPlatform();
            if(osPlatform == OSPlatform.Linux)
            {
                return Path.Combine(FetcherDownloadPath, "");
            }
            else if(osPlatform == OSPlatform.OSX)
            {
                var macFolder = Directory.GetDirectories(FetcherDownloadPath).FirstOrDefault(x => x.ToLowerInvariant().Contains("mac"));
                return Path.Combine(FetcherDownloadPath, $@"{macFolder}/chrome-mac/Chromium.app/Contents/MacOS/Chromium");
            }
            else if (osPlatform == OSPlatform.Windows)
            {
                var winFolder = Directory.GetDirectories(FetcherDownloadPath).FirstOrDefault(x => x.ToLowerInvariant().Contains("win"));
                return Path.Combine(FetcherDownloadPath, $@"{winFolder}/chrome-win/Chrome.exe");
            }

            throw new FileNotFoundException();
        }
    }
}