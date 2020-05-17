using System.IO;
using System;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Infraestructure.PupeteerSupport;
using PuppeteerSharp;

namespace Mega.WhatsAppAutomator.Infraestructure
{
    public static class AutomationStartup
    {
        private static string ScreenshotsDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");

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
                await LoginOnEmulator(page);
            }

            await StartQueueAsync(page);
            await StartListeningToMessagesAsync();
        }

        private static async Task StartListeningToMessagesAsync()
        {
            await Task.Run(() => { });
        }

        private static async Task StartQueueAsync(Page page)
        {
            await AutomationQueue.StartQueueAsync(page);
        }

        private static async Task<bool> CheckItsLoggedIn(Page page)
        {
            var mainDiv = await page.QuerySelectorAsync(WhatsAppWebMetadata.SelectorMainDiv);
            return mainDiv == null;
        }

        private static async Task LoginOnEmulator(Page page)
        {
            await Task.Run(() => { });
        }

        private static async Task NavigateToWhatsAppWebPage(Page page)
        {
            if(Headless)
            {
                await page.SetUserAgentAsync(PupeteerMetadata.CustomUserAgentForHeadless);
            }

            await page.GoToAsync(WhatsAppWebMetadata.WhatsAppURL);
        }

        public static async Task GetQrCode(Page page)
        {
            await page.WaitForSelectorAsync(WhatsAppWebMetadata.SelectorMainDiv, new WaitForSelectorOptions { Timeout = (int?)Convert.ToInt32(TimeSpan.FromSeconds(10).TotalMilliseconds) });
            
            if(!Directory.Exists(ScreenshotsDirectory))
            {
                Directory.CreateDirectory(ScreenshotsDirectory);
            }

            var fileName = Path.Combine(ScreenshotsDirectory, "QrCode.jpg");
            await page.ScreenshotAsync(fileName, new ScreenshotOptions { Clip = await page.GetElementClipAsync(WhatsAppWebMetadata.SelectorMainDiv) });
        }
    }
}
