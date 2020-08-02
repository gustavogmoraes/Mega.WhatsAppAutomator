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
            if(message.Number == "+551291828152")
            {
                message.Number = "+5512991828152";
            }
            var openChatExpression = WhatsAppWebMetadata.SendMessageExpression(message.Number);            
            await page.EvaluateExpressionAsync(openChatExpression);
            Thread.Sleep(TimeSpan.FromSeconds(0.5));

            //var teste = await page.QuerySelectorAsync(WhatsAppWebMetadata.AcceptInvalidNumber);
            //if (teste != null)
            //{
            //    await page.ClickOnElementAsync(WhatsAppWebMetadata.AcceptInvalidNumber);
            //    return;
            //}
            if (await CheckIfNumberExists(page))
			{
                //Thread.Sleep(TimeSpan.FromSeconds(0.5));
				await page.ClickAsync(WhatsAppWebMetadata.AcceptInvalidNumber);
				await StoreNotDeliveredMessage(message);
				return;
			}

			await SendHumanizedMessage(page, message.Text, message.Number);
        }

        private static async Task SendHumanizedMessage(Page page, string messageText, string number)
        {
            Humanizer = AutomationQueue.ClientConfiguration.HumanizerConfiguration;
            var clientName = "Laboratório HLAGyn";
            var random = new Random();

            if (!GetCollaboratorNumbers().Contains(number))
            {
                // Greetings
                await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, GetHumanizedGreeting(clientName));
                await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
                Thread.Sleep(TimeSpan.FromSeconds(random.Next(1, 3)));
                //
            }
            
            // The message
            await page.WaitForSelectorAsync(WhatsAppWebMetadata.ChatContainer);
            await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, messageText, 0, true);
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
            if (!GetCollaboratorNumbers().Contains(number))
            {
                Thread.Sleep(TimeSpan.FromSeconds(random.Next(1, 3)));
            }
            //

            if (!GetCollaboratorNumbers().Contains(number))
            {
                // Farewell
                await page.WaitForSelectorAsync(WhatsAppWebMetadata.ChatContainer);
                await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, GetHumanizedFarewell(clientName));
                await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
                //
            }
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