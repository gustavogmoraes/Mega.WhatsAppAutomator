using System.IO;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using PuppeteerSharp;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Mega.WhatsAppAutomator.Infrastructure.TextNow;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using PuppeteerSharp.Contrib.Extensions;
using PuppeteerSharp.Mobile;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class AutomationStartup
    {
        public static bool SmsReady { get; set; }

        public static async Task StartSms()
        {
            var runningInDocker = PupeteerMetadata.AmIRunningInDocker;
            if (!runningInDocker)
            {
                await new BrowserFetcher(PupeteerMetadata.FetcherOptions).DownloadAsync(BrowserFetcher.DefaultRevision);
            }
            
            Console.WriteLine($"Trying to launch");
            var launchOptions = PupeteerMetadata.GetLaunchOptions();

            var browser = await Puppeteer.LaunchAsync(launchOptions);
            var page = await browser.NewPageAsync();
            Console.WriteLine($"Launched, now going to page");

            await NavigateToTextNowPage(page);
            
            //await Portabilidade.Start(browser);
            SmsReady = true;
            StartQueue(page);
        }
        
        public static async Task Start()
        {
            var runningInDocker = PupeteerMetadata.AmIRunningInDocker;
            // Only necessary if not running on container, as the docker image setup downloads the browser and it's dependencies
            // followed the PuppeteerSharp's creator guide for docker builds posted at http://www.hardkoded.com/blog/puppeteer-sharp-docker
            if (!runningInDocker)
            {
                await new BrowserFetcher(PupeteerMetadata.FetcherOptions).DownloadAsync(BrowserFetcher.DefaultRevision);
            }
            
            Console.WriteLine($"Trying to launch");
            var browser = await Puppeteer.LaunchAsync(PupeteerMetadata.GetLaunchOptions());
            var page = await browser.NewPageAsync();
            Console.WriteLine($"Launched, now going to page");
            await NavigateToWhatsAppWebPage(page);

            var amILogged = await CheckItsLoggedIn(page);
            Console.WriteLine(amILogged ? "I am logged" : "I AM NOT LOGGED IN");
            if(!amILogged)
            {
                await GetQrCode(page);
                Console.WriteLine("Waiting for QrCode scan");
                await WaitForSetup(page, TimeSpan.FromMinutes(5));
            }

            StartQueue(page);
            //await StartListeningToMessagesAsync();
            //StartListeningToMessagesAsync(page);
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

		private static async Task StartListeningToMessagesAsync(Page page)
		{

			await page.ClickOnElementAsync(WhatsAppWebMetadata.Unread);

			await page.ExposeFunctionAsync("newChat", async (string text) =>
			{
				Console.WriteLine(text);
			});
<<<<<<< Updated upstream
=======

            //document.querySelector("#main > div._3h-WS > div > div > div.z_tTQ > div:nth-child(NUMBER) > div > div > div > div.copyable-text > div > span._3Whw5.selectable-text.invisible-space.copyable-text > span")

            
>>>>>>> Stashed changes
        }

        private static void StartQueue(Page page)
        {
            AutomationQueue.StartQueue(page);
        }

        private static async Task<bool> CheckItsLoggedIn(Page page)
        {
            try
            {
                _ = await page.WaitForSelectorAsync(WhatsAppWebMetadata.SelectorMainDiv, new WaitForSelectorOptions { Timeout = Convert.ToInt32(TimeSpan.FromSeconds(10).TotalMilliseconds) });
                return false;
            }
            catch (Exception e)
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
            await page.GoToAsync(WhatsAppWebMetadata.WhatsAppURL);
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

            await page.TypeOnElementAsync("#identifierId", "gustavogmoraes2");
            await page.ClickOnElementAsync("#identifierNext > div > button > div.VfPpkd-RLmnJb");

            var passwordSelector = "#password > div.aCsJod.oJeWuf > div > div.Xb9hP > input";
            await page.WaitForSelectorAsync(passwordSelector);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            await page.TypeOnElementAsync(passwordSelector, "Gustavo26@");
            await page.ClickOnElementAsync("#passwordNext > div > button > div.VfPpkd-RLmnJb");
        }

        public static async Task GetQrCode(Page page)
        {
            Console.WriteLine("Getting QrCode");
            await page.WaitForSelectorAsync(WhatsAppWebMetadata.SelectorMainDiv, new WaitForSelectorOptions { Timeout = (int?)Convert.ToInt32(TimeSpan.FromSeconds(10).TotalMilliseconds) });
            
            if(!Directory.Exists(FileManagement.ScreenshotsDirectory))
            {
                Directory.CreateDirectory(FileManagement.ScreenshotsDirectory);
            }

            var fileName = Path.Combine(FileManagement.ScreenshotsDirectory, "QrCode.jpg");
            Console.WriteLine($"Saved file at {fileName}");
            await page.ScreenshotAsync(fileName, new ScreenshotOptions { Clip = await page.GetElementClipAsync(WhatsAppWebMetadata.SelectorMainDiv) });
        }
    }
}
