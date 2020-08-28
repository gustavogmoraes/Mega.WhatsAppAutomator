using System.Linq;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.AccessControl;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using PuppeteerSharp;
using Extensions = PuppeteerSharp.Extensions;

namespace Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport
{
    public static class PupeteerMetadata
    {
        public static string CustomUserAgentForHeadless => @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.89 Safari/537.36";

        private static string[] CustomsArgsForHeadless => new []
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

        private static readonly string[] UserDataExceptions =
        {
            @"Default/Web Data", 
            @"Default/Login Data",
            @"Default/Last Session",
            @"Default/Cookies",
            @"Default/Cookies-journal",
            @"Default/Login Data-journal",
            @"Default/Web Data-journal",
            @"Default/Cache",
            @"Default/Code Cache",
            @"Default/Local Storage"
        };

        public static IList<string> UserDataDirDirectoriesAndFilesExceptionsToNotDelete =>
            UserDataExceptions.Select(x => Path.Combine(UserDataDir, x)).ToList();

        private static bool _didAlreadyProcessUserDataDir;
        private static string _processedUserDataDir;
        public static string UserDataDir
        {
            get
            {
                if (_didAlreadyProcessUserDataDir)
                {
                    return _processedUserDataDir;
                }
                
                var browserFilesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserFiles");
                var userDataDirPath = Path.Combine(browserFilesDir, "user-data-dir");
                if(!Directory.Exists(browserFilesDir))
                {
                    var directoryInfo = Directory.CreateDirectory(browserFilesDir);
                    var osPlat = DevOpsHelper.GetOsPlatform();
                    if (osPlat != OSPlatform.Windows)
                    {
                        DevOpsHelper.Bash($"chmod 755 {browserFilesDir}");
                    }
                    else
                    {
                        new DirectoryInfo(browserFilesDir).GetPermission();
                    }
                }

                if (Directory.Exists(userDataDirPath))
                {
                    _processedUserDataDir = userDataDirPath;
                    _didAlreadyProcessUserDataDir = true;
                    return userDataDirPath;
                }

                var instanceId = EnvironmentConfiguration.InstanceId;
                var zipPath = userDataDirPath + ".zip";
                
                Console.WriteLine("Trying to download user data file");
                using var session = Stores.MegaWhatsAppApi.OpenSession();
                var client = session.Query<Client>().FirstOrDefault(x => x.Token == "23ddd2c6-e46c-4030-9b65-ebfc5437d8f1");
                var att = session.Advanced.Attachments.Get(client, $"{instanceId}.zip");
                if (att != null)
                {
                    Console.WriteLine($"Found file, downloading it {att.Details.Size.GetReadableFileSize()}");
                    att.Stream.SaveStreamAsFile(zipPath);

                    ZipFile.ExtractToDirectory(zipPath, userDataDirPath);
                
                    File.Delete(zipPath);

                    _processedUserDataDir = userDataDirPath;
                    _didAlreadyProcessUserDataDir = true;
                    return userDataDirPath;
                }
                
                Console.WriteLine("Did not find user data file, creating new");

                _processedUserDataDir = userDataDirPath;
                _didAlreadyProcessUserDataDir = true;
                return userDataDirPath;
            }
        }

        public static BrowserFetcherOptions FetcherOptions => new BrowserFetcherOptions { Path = FetcherDownloadPath };
        private static string FetcherDownloadPath => Path.Combine(PupeteerBasePath, "Browser");
        private static string PupeteerBasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PupeteerFiles");
        private static string ExecutablePath => SolveExecutablePath();
        public static bool AmIRunningInDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        public static bool Headless => AmIRunningInDocker || EnvironmentConfiguration.UseHeadlessChromium;

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