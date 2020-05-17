using System.Linq;
using System.Runtime.InteropServices;
using System;
using System.IO;
using Mega.WhatsAppAutomator.Infraestructure.DevOps;
using PuppeteerSharp;

namespace Mega.WhatsAppAutomator.Infraestructure.PupeteerSupport
{
    public class PupeteerMetadata
    {
        public static string CustomUserAgentForHeadless => "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3312.0 Safari/537.36";
        public static string[] CustomsArgsForHeadless => new string[] { "--disable-gpu", "--no-sandbox", "--disable-extensions", "--proxy-server='direct://'", "--proxy-bypass-list=*" };
        public static string UserDataDir => Path.Combine(PupeteerBasePath, "user-data-dir");
        public static BrowserFetcherOptions FetcherOptions => new BrowserFetcherOptions { Path = FetcherDownloadPath };
        public static string FetcherDownloadPath => Path.Combine(PupeteerBasePath, "Browser");
        public static string PupeteerBasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PupeteerFiles");
        public static string ExecutablePath => SolveExecutablePath();

        public static LaunchOptions GetLaunchOptions(bool headless) => new LaunchOptions
        {
            Headless = headless,
            UserDataDir = PupeteerMetadata.UserDataDir,
            ExecutablePath = PupeteerMetadata.ExecutablePath
        };

        private static string SolveExecutablePath()
        {
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
                
            }

            throw new FileNotFoundException();
        }
    }
}