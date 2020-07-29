using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using PuppeteerSharp;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class WhatsAppWebTasks
    {
        public static async Task SendMessage(Page page, Message message)
        {
            // Opens the chat
            var openChatExpression = WhatsAppWebMetadata.SendMessageExpression(message.Number);
            await page.EvaluateExpressionAsync(openChatExpression);
            //

            await SendHumanizedMessage(page, message.Text);

            // await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, " " + message.Text);
            //
            // await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
        }

        private static async Task SendHumanizedMessage(Page page, string messageText)
        {
            var clientName = "Laboratório HLAGyn";
            
            // Greetings
            await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, GetHumanizedGreeting(clientName));
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
            Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, 5)));
            //
            
            // The message
            await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, messageText);
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
            Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, 5)));
            //
            
            // Farewell
            await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, GetHumanizedFarewell(clientName));
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
            //
        }

        private static readonly string[] Greetings = { "Oi", "Olá", "Saudações" };

        private static readonly string[] Cumpliments = { string.Empty, "tudo bem?", "espero que esteja bem", "como vai?", "tudo joia?" };
        
        private static readonly string[] ClientPresentations = 
            { string.Empty, "Falo do {clientName}", "Aqui é do {clientName}", "Represento o {clientName}" };
        
        private static readonly string[] Farewells = 
            {  "Agradeço pela atenção", "Obrigado", "Estamos a seu dispor" };

        private static string GetClientPresentation(string clientName)
        {
            return ClientPresentations.Random().Replace("clientName", clientName);
        }
        
        private static string GetHumanizedGreeting(string clientName)
        {
            return $" {Greetings.Random()} {Cumpliments.Random()}/r/n" +
                   $"{GetClientPresentation(clientName)}";
        }
        
        private static string GetHumanizedFarewell(string clientName)
        {
            return Farewells.Random();
        }
    }
}