using System.IO;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using PuppeteerSharp;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Raven.Client.Documents;
using static Mega.WhatsAppAutomator.Infrastructure.Utils.Extensions;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class AutomationStartup
    {
        public static bool SmsReady { get; set; }
        
        public static Browser BrowserRef { get; set; }

        public static async Task StartSms()
        {
            var runningInDocker = PupeteerMetadata.AmIRunningInDocker;
            if (!runningInDocker)
            {
                await new BrowserFetcher(PupeteerMetadata.FetcherOptions).DownloadAsync(BrowserFetcher.DefaultRevision);
            }
            
            WriteOnConsole($"Trying to launch");
            var launchOptions = PupeteerMetadata.GetLaunchOptions();

            var browser = await Puppeteer.LaunchAsync(launchOptions);
            var page = await browser.NewPageAsync();
            WriteOnConsole($"Launched, now going to page");

            StartQueue(page);
        }
        public static void ExitApplication()
        {
            Task.Run(() =>
            {
                WriteOnConsole("Received stop request, after the running cycle the application will stop");
                AutomationQueue.StopBrowser = true;
            });

            while (!AutomationQueue.ReadyToBeShutdown)
            {
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
            
            WriteOnConsole("Application has been shut down gracefully");
        }
        
        public static async Task Start()
        {
            //// Only necessary if not running on container, as the docker image setup downloads the browser and it's dependencies
            //// followed the PuppeteerSharp's creator guide for docker builds posted at http://www.hardkoded.com/blog/puppeteer-sharp-docker
            var runningInDocker = PupeteerMetadata.AmIRunningInDocker;
            if (!runningInDocker)
            {
                WriteOnConsole("Not running on Docker, checking and downloading browser/dependencies");
                await new BrowserFetcher(PupeteerMetadata.FetcherOptions).DownloadAsync(BrowserFetcher.DefaultRevision);
            }
            
            WriteOnConsole("Trying to launch");
            var browser = await Puppeteer.LaunchAsync(PupeteerMetadata.GetLaunchOptions());
            BrowserRef = browser;

            await GetMetadata();

            var page = await browser.NewPageAsync();
            WriteOnConsole($"Launched, now going to page");
            await NavigateToWhatsAppWebPage(page);

            var amILogged = await CheckItsLoggedIn(page);
            WriteOnConsole(amILogged ? "I am logged" : "I AM NOT LOGGED IN");
            if (!amILogged)
            {
                await GetQrCode(page);
                WriteOnConsole("Waiting for QrCode scan");
                await WaitForSetup(page, TimeSpan.FromMinutes(5));
            }

            StartQueue(page);
        }

        private static async Task GetMetadata()
        {
            var session = Stores.MegaWhatsAppApi.OpenAsyncSession();
            Config.WhatsAppWebMetadata = await session.Query<WhatsAppWebMetadata>().FirstOrDefaultAsync();
        }

        private static async Task WaitForSetup(Page page, TimeSpan timeout)
        {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                while (!await CheckItsLoggedIn(page))
                {
                    if (stopwatch.Elapsed > timeout)
                    {
                        throw new TimeoutException("Setup has timed out");
                    }

                    Thread.Sleep(500);
                }
        }
		// private static async Task StartListeningToMessagesAsync(Page page)
		// {
        // 	await page.ClickOnElementAsync(WhatsAppWebMetadata.Unread);
        // 	await page.ExposeFunctionAsync("newChat", async (string text) =>
		// 	{
		// 		WriteOnConsole(text);
		// 	});
        //}

        private static void StartQueue(Page page) => AutomationQueue.StartQueue(page);

        private static async Task<bool> CheckItsLoggedIn(Page page)
		{
			try
			{
				_ = await page.WaitForSelectorAsync(
                    Config.WhatsAppWebMetadata.SelectorMainDiv, 
                    new WaitForSelectorOptions
                    {
                        Timeout = Convert.ToInt32(TimeSpan.FromSeconds(10).TotalMilliseconds)
                    });
				return false;
			}
			catch (Exception)
			{
				return true;
			}
		}
        
        private static async Task NavigateToWhatsAppWebPage(Page page)
        {
            await page.SetViewportAsync(new ViewPortOptions
            {
                IsMobile = false,
                HasTouch = false,
                Width = 1280,
                Height = 720
            });
            
            await page.SetUserAgentAsync(PupeteerMetadata.CustomUserAgentForHeadless);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            await page.GoToAsync(Config.WhatsAppWebMetadata.WhatsAppUrl);
        }
        
        private static async Task NavigateToTextNowPage(Page page)
        {
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1600,
                Height = 900,
                DeviceScaleFactor = 1
            });
            
            await page.SetUserAgentAsync(PupeteerMetadata.CustomUserAgentForHeadless);
            await page.GoToAsync("https://www.textnow.com/login");

            var googleLoginSelector = "#google-login";
            var amILoggedIn = await page.QuerySelectorAsync(googleLoginSelector) == null;
            if (amILoggedIn)
            {
                return;
            }
            
            await page.ClickOnElementAsync(googleLoginSelector);
            await page.WaitForNavigationAsync();

            //await page.TypeOnElementAsync("#identifierId", "gustavogmoraes2");
            await page.ClickOnElementAsync("#identifierNext > div > button > div.VfPpkd-RLmnJb");

            var passwordSelector = "#password > div.aCsJod.oJeWuf > div > div.Xb9hP > input";
            await page.WaitForSelectorAsync(passwordSelector);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            //await page.TypeOnElementAsync(passwordSelector, "Gustavo26@");
            await page.ClickOnElementAsync("#passwordNext > div > button > div.VfPpkd-RLmnJb");
        }

        private static async Task GetQrCode(Page page)
        {
            WriteOnConsole("Getting QrCode");
            Thread.Sleep(5000);
            await page.WaitForSelectorAsync(
                Config.WhatsAppWebMetadata.SelectorMainDiv, 
                new WaitForSelectorOptions
                {
                    Timeout = Convert.ToInt32(TimeSpan.FromSeconds(10).TotalMilliseconds)
                });
            Thread.Sleep(TimeSpan.FromSeconds(3));
            if(!Directory.Exists(FileManagement.ScreenshotsDirectory))
            {
                Directory.CreateDirectory(FileManagement.ScreenshotsDirectory);
            }

            var fileName = Path.Combine(FileManagement.ScreenshotsDirectory, "QrCode.jpg");
            WriteOnConsole($"Saved file at {fileName}");
            await page.ScreenshotAsync(fileName, new ScreenshotOptions { Clip = await page.GetElementClipAsync(Config.WhatsAppWebMetadata.SelectorMainDiv) });
        }
    }
}
