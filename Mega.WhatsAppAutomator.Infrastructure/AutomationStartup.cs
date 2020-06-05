using System.IO;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using PuppeteerSharp;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class AutomationStartup
    {
        private static bool Headless => false;

        public static async Task Start()
        {
            await new BrowserFetcher(PupeteerMetadata.FetcherOptions).DownloadAsync(BrowserFetcher.DefaultRevision);

            var browser = await Puppeteer.LaunchAsync(PupeteerMetadata.GetLaunchOptions(Headless));
            var page = await browser.NewPageAsync();

            await NavigateToWhatsAppWebPage(page);

            if(!await CheckItsLoggedIn(page))
            {
                await GetQrCode(page);
                await WaitForSetup(page, TimeSpan.FromMinutes(5));
            }

            StartQueue(page);
            //await StartListeningToMessagesAsync();
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

        private static async Task StartListeningToMessagesAsync()
        {
            await Task.Run(() => { });
        }

        private static void StartQueue(Page page)
        {
            AutomationQueue.StartQueue(page);
        }

        private static async Task<bool> CheckItsLoggedIn(Page page)
        {
            try
            {
                _ = await page.WaitForSelectorAsync(WhatsAppWebMetadata.SelectorMainDiv, new WaitForSelectorOptions { Timeout = Convert.ToInt32(TimeSpan.FromSeconds(3).TotalMilliseconds) });
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
                Width = 1280,
                Height = 720,
                DeviceScaleFactor = 1,
                IsLandscape = true,
                IsMobile = false
            });
            
            await page.SetUserAgentAsync(PupeteerMetadata.CustomUserAgentForHeadless);
            await page.GoToAsync(WhatsAppWebMetadata.WhatsAppURL);
        }

        public static async Task GetQrCode(Page page)
        {
            await page.WaitForSelectorAsync(WhatsAppWebMetadata.SelectorMainDiv, new WaitForSelectorOptions { Timeout = (int?)Convert.ToInt32(TimeSpan.FromSeconds(10).TotalMilliseconds) });
            
            if(!Directory.Exists(FileManagement.ScreenshotsDirectory))
            {
                Directory.CreateDirectory(FileManagement.ScreenshotsDirectory);
            }

            var fileName = Path.Combine(FileManagement.ScreenshotsDirectory, "QrCode.jpg");
            await page.ScreenshotAsync(fileName, new ScreenshotOptions { Clip = await page.GetElementClipAsync(WhatsAppWebMetadata.SelectorMainDiv) });
        }
    }
}
