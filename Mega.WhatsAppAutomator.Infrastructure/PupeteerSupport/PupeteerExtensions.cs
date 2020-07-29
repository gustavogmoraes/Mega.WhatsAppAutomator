using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using PuppeteerSharp.Media;

namespace Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport
{
    public static class PupeteerExtensions
    {
        public static async Task<Clip> GetElementClipAsync(this Page page, string elementSelector)
        {
            var jTokenResult = await page.EvaluateFunctionAsync(JavaScriptFunctions.GetBoudingClientRect, elementSelector);
            var rectangle = jTokenResult.ToObject<dynamic>();

            return new Clip
            {
                X = rectangle.x,
                Y = rectangle.y,
                Width = rectangle.width,
                Height = rectangle.height
            };
        }

        public static async Task SaveCookiesAsync(this Page page, string cookiesPath) 
        {
            // This gets all cookies from all URLs, not just the current URL
            var client = await page.Target.CreateCDPSessionAsync();

            var cookiesJToken = (await client.SendAsync("Network.getAllCookies"))["cookies"];
            var cookiesDynamic = ((JArray)cookiesJToken).ToObject<CookieParam[]>();

            await File.WriteAllTextAsync(cookiesPath, JsonConvert.SerializeObject(cookiesDynamic));
        }

        public static async Task RestoreCookiesAsync(this Page page, string cookiesPath) 
        {
            try 
            {
                var cookiesJson = await File.ReadAllTextAsync(cookiesPath);
                var cookies = JsonConvert.DeserializeObject<CookieParam[]>(cookiesJson);

                await page.SetCookieAsync(cookies);
            } 
            catch (Exception err) 
            {
                Console.WriteLine("Restore cookie error", err);
            }
        }

        public static async Task TypeOnElementAsync(this Page page, string elementSelector, string text, int? delayInMs = null, bool useParser = false)
        {
            if (!useParser)
            {
                await page.WaitForSelectorAsync(elementSelector);
                
                var element = await page.QuerySelectorAsync(elementSelector);
                await element.TypeAsync(text, new TypeOptions { Delay = delayInMs ?? GetRandomDelay() });
                
                return;
            }

            var pieces = text.Split("\n\r").ToList();
            foreach (var piece in pieces)
            {
                await page.WaitForSelectorAsync(elementSelector);
                
                var element = await page.QuerySelectorAsync(elementSelector);
                await element.TypeAsync(piece, new TypeOptions { Delay = delayInMs });

                await page.PressShiftEnterAsync();
            }
        }

        // This is in ms
        private static int GetRandomDelay()
        {
            return new Random().Next(50, 150);
        }

		public static async Task ClickOnElementAsync(this Page page, string elementSelector)
		{
			//await (await page.QuerySelectorAsync(elementSelector))?.ClickAsync();

			var element = await page.QuerySelectorAsync(elementSelector);
			if (element != null)
			{
				await element.ClickAsync();
			}
		}
        
        public static async Task PressShiftEnterAsync(this Page page)
        {
            await page.Keyboard.DownAsync(Key.Shift);
            await page.Keyboard.PressAsync(Key.Enter);
            await page.Keyboard.UpAsync(Key.Shift);
        }
	}
}
