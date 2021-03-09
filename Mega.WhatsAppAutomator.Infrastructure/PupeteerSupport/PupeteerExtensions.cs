using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Domain.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using PuppeteerSharp.Media;
using TextCopy;
using static Mega.WhatsAppAutomator.Infrastructure.Utils.Extensions;

namespace Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport
{
    public static class PupeteerExtensions
    {
        public static async Task<Clip> GetElementClipAsync(this Page page, string elementSelector)
        {
            var jTokenResult = await page.EvaluateFunctionAsync(JavaScriptFunctions.GetBoudingClientRect, elementSelector);
            
            var rectangle = jTokenResult.ToObject<dynamic>();
            if (rectangle == null)
            {
	            throw new NullReferenceException("Could not find the element on screen");
            }
            
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
                WriteOnConsole("Restore cookie error" + err.Message);
            }
        }

        public static async Task PasteOnElementAsync(this Page page, string elementSelector, string text)
        {
            var temp = await ClipboardService.GetTextAsync();
            var pieces = text.Split(new string[] { "\r\n", "\n\r" }, StringSplitOptions.None).ToList();
            var message = String.Join('\n', pieces);
            await ClipboardService.SetTextAsync(message);
            await page.FocusAsync(elementSelector);
            await page.PressControlPaste();
            await ClipboardService.SetTextAsync(temp);
        }
        
        public static async Task TypeOnElementAsync(
            this Page page, 
            string elementSelector, 
            string text,
            MessagePhase phase,
            bool useParser = false)
        {
            var element = await page.QuerySelectorAsync(elementSelector);
            await element.ClickAsync();
            
            Thread.Sleep(new Random().Next(500, 800));
            
            if (!useParser && phase != null)
            {
                await element.TypeAsync(text, new TypeOptions { Delay = GetRandomTypingDelay(phase) });
                
                return;
            }
            
            // TODO: Review these
            var newText = text.Replace("\n\r", "\r\n");
            var pieces = newText.Split(new[] {"\r\n"}, StringSplitOptions.None)
                .Select(x => x.Trim())
                .ToImmutableList();

            foreach (var piece in pieces)
            {
                var randomDelay = phase == null 
                    ? 0
                    : GetRandomTypingDelay(phase);
                
                await element.TypeAsync(piece, new TypeOptions { Delay = randomDelay });
                
                await page.PressShiftEnterAsync();

                Thread.Sleep(new Random().Next(100, 300));
            }
        }
        
        public static int GetRandomTypingDelay(MessagePhase phase)
        {
            return new Random().Next(phase.MinTypingDelay, phase.MaxTypingDelay);
        }
        
        public static int GetRandomWaitTime(MessagePhase phase)
        {
            return new Random().Next(phase.MinWaitTimeAfter, phase.MaxWaitTimeAfter);
        }

		public static async Task ClickOnElementAsync(this Page page, string elementSelector)
		{
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
        
        public static async Task PressControlPaste(this Page page)
        {
            await page.Keyboard.DownAsync(Key.Control);
            await page.Keyboard.PressAsync("V");
            await page.Keyboard.UpAsync(Key.Control);
        }
    }
}
