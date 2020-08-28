using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using Raven.Client;
using Raven.Client.Documents.Operations.Configuration;
using Raven.Client.ServerWide.Operations;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class WhatsAppWebTasks
    {
        private static HumanizerConfiguration Humanizer { get; set; }
        
        public static async Task SendMessage(Page page, Message message)
        {
            var messageNumber = message.Number;
            TreatStrangeNumbers(ref messageNumber);

            await OpenChat(page, messageNumber);

            if (!await CheckIfNumberExists(page, message.Number))
			{
                await page.ClickAsync(WhatsAppWebMetadata.AcceptInvalidNumber);
				await StoreNotDeliveredMessage(message);
				return;
			}

			await SendHumanizedMessage(page, message.Text, messageNumber);
        }

        public static async Task<bool> SendMessageGroupedByNumber(Page page, string number, List<string> listOfTexts)
        {
            TreatStrangeNumbers(ref number);

            await SendHumanizedMessageByNumberGroups(page, number, listOfTexts);
            return true;
        }
        
        private static async Task SendHumanizedMessageByNumberGroups(Page page, string number, List<string> texts)
        {
            Humanizer = AutomationQueue.ClientConfiguration.HumanizerConfiguration;
            var clientName = "Laboratório HLAGyn";
            var random = new Random();

            var useHumanizationMessages = UseHumanizationMessages(number);
            
            // Greetings
            if (useHumanizationMessages) { await SendGreetings(page, clientName, random); }

            // Message
            await SendGroupOfMessages(page, texts, random);
            if (useHumanizationMessages) { Thread.Sleep(TimeSpan.FromSeconds(random.Next(Humanizer.MinimumDelayAfterMessage, Humanizer.MaximumDelayAfterMessage))); }
            
            // Farewell
            if (useHumanizationMessages) { await SendFarewell(page, clientName); }
        }
        
        private static bool UseHumanizationMessages(string number) =>
            !Humanizer.InsaneMode && 
            Humanizer.UseHumanizer && 
            !GetCollaboratorNumbers().Contains(number);

        public static async Task DismissErrorAndStoreNotDelivereds(Page page, string number, List<ToBeSent> intendeds)
        {
            await page.ClickAsync(WhatsAppWebMetadata.AcceptInvalidNumber);
            foreach (var intend in intendeds)
            {
                await StoreNotDeliveredMessage(intend.Message);
            }
        }
        private static void TreatStrangeNumbers(ref string number)
        {
            if(number == "+551291828152")
            {
                number = "+5512991828152";
            }
        }
        
        private static async Task OpenChat(Page page, string number)
        {
            var openChatExpression = WhatsAppWebMetadata.SendMessageExpression(number);
            await page.EvaluateExpressionAsync(openChatExpression);
            Thread.Sleep(TimeSpan.FromSeconds(0.5));
        }
        private static async Task SendHumanizedMessage(Page page, string messageText, string number)
        {
            Humanizer = AutomationQueue.ClientConfiguration.HumanizerConfiguration;
            var clientName = "Laboratório HLAGyn";
            var random = new Random();
            
            // Greetings
            if (!GetCollaboratorNumbers().Contains(number) || !Humanizer.UseHumanizer)
            {
                await SendGreetings(page, clientName, random);
            }
            
            // Message
            await SendMessage(page, messageText, random);
            if (!GetCollaboratorNumbers().Contains(number) || !Humanizer.UseHumanizer)
            {
                Thread.Sleep(TimeSpan.FromSeconds(random.Next(Humanizer.MinimumDelayAfterMessage, Humanizer.MaximumDelayAfterMessage)));
            }
            
            // Farewell
            if (!GetCollaboratorNumbers().Contains(number) || !Humanizer.UseHumanizer)
            {
                await SendFarewell(page, clientName);
            }
        }

        private static async Task SendFarewell(Page page, string clientName)
        {
            await page.WaitForSelectorAsync(WhatsAppWebMetadata.ChatContainer);
            await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, GetHumanizedFarewell(clientName));
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
        }

        private static async Task SendMessage(Page page, string messageText, Random random, bool sendAfterTyping = true)
        {
			await page.WaitForSelectorAsync(WhatsAppWebMetadata.ChatContainer);
			await page.TypeOnElementAsync(
				WhatsAppWebMetadata.ChatContainer,
				RandomSpaceBetweenWords(messageText),
				delayInMs: Humanizer.InsaneMode ? 0 : random.Next(Humanizer.MinimumMessageTypingDelay, Humanizer.MaximumMessageTypingDelay),
				useParser: true);
			if (sendAfterTyping)
			{
				await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
			}
		}
        
        private static async Task SendGroupOfMessages(Page page, List<string> texts, Random random)
        {
            var finalText = string.Empty;

            foreach (var text in texts)
            {
                finalText += text;
                if (text.Contains("\r\n") || text.Contains("\r\n"))
                {
                    finalText += "\n"; // this will send the message.
                    continue;
                }

                finalText += "\r\n";
            }
            
            await SendMessage(page, finalText, random);
        }

        private static async Task SendGreetings(Page page, string clientName, Random random)
        {
            await page.TypeOnElementAsync(
                WhatsAppWebMetadata.ChatContainer,
                GetHumanizedGreeting(clientName),
                random.Next(Humanizer.MinimumGreetingTypingDelay, Humanizer.MaximumGreetingTypingDelay));
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
            Thread.Sleep(TimeSpan.FromSeconds(random.Next(Humanizer.MinimumDelayAfterGreeting, Humanizer.MaximumDelayAfterGreeting)));
        }
        
        private static string GetClientPresentation(string clientName)
        {
            return Humanizer.ClientPresentationsPool.Random().Replace("{clientName}", clientName);
        }
        
        private static string GetHumanizedGreeting(string clientName)
        {
            return $"{Humanizer.GreetingsPool.Random()} {Humanizer.CumplimentsPool.Random()}\r\n" +
                   $"{GetClientPresentation(clientName)}";
        }
        
        private static string GetHumanizedFarewell(string clientName)
        {
            return Humanizer.FarewellsPool.Random();
        }
        
        private static IList<string> GetCollaboratorNumbers()
        {
            return  Humanizer.CollaboratorsContacts;
        }

		public static async Task<bool> CheckIfNumberExists(Page page, string number)
		{
			try
            {
                await OpenChat(page, number);
				await page.WaitForSelectorAsync(WhatsAppWebMetadata.AcceptInvalidNumber, new WaitForSelectorOptions { Visible = true, Timeout = Convert.ToInt32(TimeSpan.FromSeconds(2).TotalMilliseconds) });

				return false;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static async Task StoreNotDeliveredMessage(Message erroMessage)
        {
            using var session = Stores.MegaWhatsAppApi.OpenAsyncSession();
            await session.StoreAsync(new NotDelivered
            {
                Message = erroMessage,
                ExecutionTime = DateTime.UtcNow.ToBraziliaDateTime()
            });
            await session.SaveChangesAsync();
        }

        private static string RandomSpaceBetweenWords(string messageText) 
        {
            var space = new[] { " ", "  ", " ", " ", " ", " "};
            var teste = messageText.Split(' ');
            var resultado = string.Empty;
            foreach (var t in teste)
            {
                resultado += t + space.Random();
            }
            return resultado;
        }

      
    }
}