using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Mega.WhatsAppAutomator.Infraestructure.PupeteerSupport
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
    }
}
