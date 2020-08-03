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
    public static class PupeteerMetadata
    {
        public static string CustomUserAgentForHeadless => @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.89 Safari/537.36";
        
        public static string[] CustomsArgsForHeadless => new []
        {
            // "--proxy-server='direct://'",
            // "--proxy-bypass-list=*",
            // "--use-fake-ui-for-media-stream",
            "--log-level=3",
            "--no-default-browser-check",
            "--disable-infobars",
            "--disable-notifications",
            "--disable-web-security",
            "--disable-site-isolation-trials",
            "--no-experiments",
            "--ignore-gpu-blacklist",
            "--ignore-certificate-errors",
            "--ignore-certificate-errors-spki-list",
            //"--disable-gpu",
            "--disable-extensions",
            "--disable-default-apps",
            "--enable-features=NetworkService",
            "--disable-setuid-sandbox",
            "--no-sandbox"
        };
       
        // "--window-position=0,0",
        // "--ignore-certifcate-errors",
        // "--ignore-certifcate-errors-spki-list",
        
        private static string UserDataDir
        {
            get
            {
                var browserFilesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserFiles");
                if(!Directory.Exists(browserFilesDir))
                {
                    var directoryInfo = Directory.CreateDirectory(browserFilesDir);
                    directoryInfo.Attributes &= ~FileAttributes.ReadOnly; // Removes read-only attribute
                }
                
                return Path.Combine(browserFilesDir, "user-data-dir");
            }
        }

        public static BrowserFetcherOptions FetcherOptions => new BrowserFetcherOptions { Path = FetcherDownloadPath };
        private static string FetcherDownloadPath => Path.Combine(PupeteerBasePath, "Browser");
        private static string PupeteerBasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PupeteerFiles");
        private static string ExecutablePath => SolveExecutablePath();
        public static bool AmIRunningInDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        public static bool Headless => AmIRunningInDocker || Environment.GetEnvironmentVariable("USE_HEADLESS_CHROMIUM") == "true";

        public static LaunchOptions GetLaunchOptions()
        {
            return new LaunchOptions
            {
                Headless = Headless,
                ExecutablePath = ExecutablePath,
                Args = CustomsArgsForHeadless,
                UserDataDir = UserDataDir
            };
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