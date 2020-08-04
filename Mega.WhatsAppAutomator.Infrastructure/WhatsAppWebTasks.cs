using System;
using System.Collections;
using System.Collections.Generic;
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

            if (await CheckIfNumberExists(page))
			{
                await page.ClickAsync(WhatsAppWebMetadata.AcceptInvalidNumber);
				await StoreNotDeliveredMessage(message);
				return;
			}

			await SendHumanizedMessage(page, message.Text, messageNumber);
        }

        public static async Task SendMessageGroupedByNumber(Page page, string number, List<string> listOfTexts)
        {
            TreatStrangeNumbers(ref number);
            
            await OpenChat(page, number);
            
            if (await CheckIfNumberExists(page))
            {
                await DismissErrorAndStoreNotDelivereds(page, number, listOfTexts);
                return;
            }

            await SendHumanizedMessageByNumberGroups(page, number, listOfTexts);
        }
        
        private static async Task SendHumanizedMessageByNumberGroups(Page page, string number, List<string> texts)
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
            await SendGroupOfMessages(page, texts, random);
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

        private static async Task DismissErrorAndStoreNotDelivereds(Page page, string number, List<string> listOfTexts)
        {
            await page.ClickAsync(WhatsAppWebMetadata.AcceptInvalidNumber);
            foreach (var text in listOfTexts)
            {
                await StoreNotDeliveredMessage(new Message
                {
                    Number = number,
                    Text = text
                });
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

        private static async Task SendMessage(Page page, string messageText, Random random)
        {
            await page.WaitForSelectorAsync(WhatsAppWebMetadata.ChatContainer);
            await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, messageText, random.Next(Humanizer.MinimumMessageTypingDelay, 
                Humanizer.MaximumMessageTypingDelay), true);
            //await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
        }
        
        private static async Task SendGroupOfMessages(Page page, List<string> texts, Random random)
        {
            var finalText = string.Join("\r\n", texts);
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

		private static async Task<bool> CheckIfNumberExists(Page page)
		{
			try
			{
				await page.WaitForSelectorAsync(WhatsAppWebMetadata.AcceptInvalidNumber, new WaitForSelectorOptions { Visible = true, Timeout = Convert.ToInt32(TimeSpan.FromSeconds(2).TotalMilliseconds) });

				return true;
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
    }
}