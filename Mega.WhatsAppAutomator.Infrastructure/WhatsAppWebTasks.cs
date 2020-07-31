using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class WhatsAppWebTasks
    {
        private static HumanizerConfiguration Humanizer { get; set; }
        public static async Task SendMessage(Page page, Message message)
        {
            if(message.Number == "+551291828152")
            {
                message.Number = "+5512991828152";
            }
            var openChatExpression = WhatsAppWebMetadata.SendMessageExpression(message.Number);
            await page.EvaluateExpressionAsync(openChatExpression);

            var teste = await page.QuerySelectorAsync(WhatsAppWebMetadata.AcceptInvalidNumber);
            if (teste != null){
                await page.ClickOnElementAsync(WhatsAppWebMetadata.AcceptInvalidNumber);
                return;
            }   
            
            await SendHumanizedMessage(page, message.Text);
        }

        private static async Task SendHumanizedMessage(Page page, string messageText)
        {
            Humanizer = AutomationQueue.ClientConfiguration.HumanizerConfiguration;
            Thread.Sleep(TimeSpan.FromSeconds(1));
            var clientName = "Laboratório HLAGyn";
            
            // Greetings
            await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, GetHumanizedGreeting(clientName));
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
            Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, 5)));
            //
            
            // The message
            await page.WaitForSelectorAsync(WhatsAppWebMetadata.ChatContainer);
            await page.PasteOnElementAsync(WhatsAppWebMetadata.ChatContainer, messageText);
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
            Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, 3)));
            //
            
            // Farewell
            await page.WaitForSelectorAsync(WhatsAppWebMetadata.ChatContainer);
            await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, GetHumanizedFarewell(clientName));
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
            //
        }

        private static string GetClientPresentation(string clientName)
        {
            return Humanizer.ClientPresentationsPool.Random().Replace("{clientName}", clientName);
        }
        
        private static string GetHumanizedGreeting(string clientName)
        {
            return $"  {Humanizer.GreetingsPool.Random()} {Humanizer.CumplimentsPool.Random()}\r\n" +
                   $"{GetClientPresentation(clientName)}";
        }
        
        private static string GetHumanizedFarewell(string clientName)
        {
            return Humanizer.FarewellsPool.Random();
        }
    }
}