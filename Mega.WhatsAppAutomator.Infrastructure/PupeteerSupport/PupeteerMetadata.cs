using System.Linq;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using PuppeteerSharp;
using Raven.Client.Documents.Operations.Attachments;
using static Mega.WhatsAppAutomator.Infrastructure.Utils.Extensions;

namespace Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport
{
    public static class PupeteerMetadata
    {
        public static string CustomUserAgentForHeadless =>
            @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.89 Safari/537.36";

        private static string[] CustomsArgsForHeadless => new []
        {
            "--proxy-server='direct://'",
            "--proxy-bypass-list=*",
            "--use-fake-ui-for-media-stream",
            "--disable-gpu",
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
            "--disable-extensions",
            "--disable-default-apps",
            "--enable-features=NetworkService",
            "--disable-setuid-sandbox",
            "--no-sandbox"
        };
        
        // TODO: Review these and test, the less space we use to store data the better
        private static readonly string[] UserDataExceptions =
        {
            Path.Combine("Default", "Web Data"),
            Path.Combine("Default","Login Data"),
            Path.Combine("Default","Last Session"),
            Path.Combine("Default","Cookies"),
            Path.Combine("Default","Cookies-journal"),
            Path.Combine("Default","Login Data-journal"),
            Path.Combine("Default","Web Data-journal"),
            Path.Combine("Default", "Code Cache"),
            Path.Combine("Default", "Local Storage") // --> I think this guy is the only one necessary (Need further testing)
            //Path.Combine("Default", Cache"), --> This is the space killer
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
                
                var browserFilesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "BrowserFiles");
                var userDataDirPath = Path.Combine(browserFilesDir, "user-data-dir");
                
                if(!Directory.Exists(browserFilesDir))
                {                    
                    CreateDirectoryWithFullPermission(browserFilesDir);
                }

                if (Directory.Exists(userDataDirPath))
                {
                    _processedUserDataDir = userDataDirPath;
                    _didAlreadyProcessUserDataDir = true;
                    
                    return userDataDirPath;
                }
                
                Thread.Sleep(TimeSpan.FromSeconds(2));
                WriteOnConsole("Trying to download user-data file");
                
                var attachment = DownloadUserDataFileFromDataBase();
                if (attachment != null)
                {
                    var zipPath = userDataDirPath + ".zip";
                    ExtractUserDataFile(attachment, zipPath, userDataDirPath);

                    _processedUserDataDir = userDataDirPath;
                    _didAlreadyProcessUserDataDir = true;
                    
                    return userDataDirPath;
                }
                
                WriteOnConsole("Did not find user data file, creating new");

                _processedUserDataDir = userDataDirPath;
                _didAlreadyProcessUserDataDir = true;
                
                return userDataDirPath;
            }
        }

        private static void ExtractUserDataFile(AttachmentResult attachment, string zipPath, string userDataDirPath)
        {
            WriteOnConsole($"Found file, downloading it {attachment.Details.Size.GetReadableFileSize()}");
            attachment.Stream.SaveStreamAsFile(zipPath);

            ZipFile.ExtractToDirectory(zipPath, userDataDirPath);

            File.Delete(zipPath);
        }

        private static AttachmentResult DownloadUserDataFileFromDataBase()
        {
            using var session = Stores.MegaWhatsAppApi.OpenSession();
            var client = session.Query<Client>().FirstOrDefault(x => x.Id == EnvironmentConfiguration.ClientId);
            var attachment = session.Advanced.Attachments.Get(client, $"{EnvironmentConfiguration.InstanceId}.zip");
            
            return attachment;
        }

        private static void CreateDirectoryWithFullPermission(string browserFilesDir)
        {
            _ = Directory.CreateDirectory(browserFilesDir);
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

        public static BrowserFetcherOptions FetcherOptions => new BrowserFetcherOptions { Path = FetcherDownloadPath };
        private static string FetcherDownloadPath => Path.Combine(PupeteerBasePath, "Browser");
        private static string PupeteerBasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "PupeteerFiles");
        private static string ExecutablePath => SolveExecutablePath();
        public static bool AmIRunningInDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        private static bool Headless => AmIRunningInDocker || EnvironmentConfiguration.UseHeadlessChromium;

        public static LaunchOptions GetLaunchOptions()
        {
            return new LaunchOptions
            {
                Headless = Headless,
                ExecutablePath = ExecutablePath,
                Args = CustomsArgsForHeadless,
                UserDataDir = UserDataDir,
                SlowMo = 1
            };
        }

        private static string SolveExecutablePath()
        {
            if (AmIRunningInDocker)
            {
                WriteOnConsole($"Running in docker, returning path as {Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH")}");
                return Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
            }
            var osPlatform = DevOpsHelper.GetOsPlatform();
            if(osPlatform == OSPlatform.Linux)
            {
                // TODO
                throw new NotImplementedException();
            }

            if(osPlatform == OSPlatform.OSX)
            {
                var macFolder = Directory.GetDirectories(FetcherDownloadPath).FirstOrDefault(x => x.ToLowerInvariant().Contains("mac"));
                return Path.Combine(FetcherDownloadPath, $@"{macFolder}/chrome-mac/Chromium.app/Contents/MacOS/Chromium");
            }

            if (osPlatform == OSPlatform.Windows)
            {
                var winFolder = Directory.GetDirectories(FetcherDownloadPath).FirstOrDefault(x => x.ToLowerInvariant().Contains("win"));
                return Path.Combine(FetcherDownloadPath, $@"{winFolder}/chrome-win/Chrome.exe");
            }

            throw new Exception("OS not compatible");
        }
    }
}