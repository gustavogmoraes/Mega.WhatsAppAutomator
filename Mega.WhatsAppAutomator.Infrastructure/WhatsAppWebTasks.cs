using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using PuppeteerSharp;
using static Mega.WhatsAppAutomator.Infrastructure.Utils.Extensions;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class WhatsAppWebTasks
    {
        private static HumanizerConfiguration Humanizer { get; set; }
        
        public static async Task<bool> SendMessage(Page page, Message message)
        {
            var messageNumber = message.Number;
            TreatStrangeNumbers(ref messageNumber);

            await OpenChat(page, messageNumber);

            if (!await CheckIfNumberExists(page, message.Number))
			{
                await page.ClickAsync(Config.WhatsAppWebMetadata.AcceptInvalidNumber);
				await StoreNotDeliveredMessage(message);
				return false;
			}

			await SendHumanizedMessage(page, message.Text, messageNumber);
            return true;
        }

        public static async Task<bool> SendMessageGroupedByNumber(Page page, string number, List<string> listOfTexts)
        {
            return await SendHumanizedMessageByNumberGroups(page, number, listOfTexts);
        }
        
        private static async Task<bool> SendHumanizedMessageByNumberGroups(Page page, string number, List<string> texts, bool randommicallyDisableHumanization = true)
        {
            Humanizer = AutomationQueue.ClientConfiguration.HumanizerConfiguration;
            var random = new Random();

            var useHumanizationMessages = ShouldUseHumanizationMessages(number) && randommicallyDisableHumanization && RandomBoolean();
            // Greetings
            if (useHumanizationMessages) { await SendGreetings(page, random); }

            // Message
            await SendGroupOfMessages(page, texts, random, useHumanizationMessages);
            if (useHumanizationMessages) { Thread.Sleep(TimeSpan.FromSeconds(random.Next(Humanizer.MinimumDelayAfterMessage, Humanizer.MaximumDelayAfterMessage))); }
            
            Thread.Sleep(TimeSpan.FromSeconds(1));
            
            // Farewell
            if (useHumanizationMessages) { await SendFarewell(page, random); }

            return useHumanizationMessages;
        }

        private static bool RandomBoolean()
        {
            return new Random().Next(2) == 0;
        }

        private static bool ShouldUseHumanizationMessages(string number) =>
            !Humanizer.InsaneMode && 
            Humanizer.UseHumanizer && 
            !GetCollaboratorNumbers().Contains(number);

        public static async Task DismissErrorAndStoreNotDelivereds(Page page, List<ToBeSent> intendeds)
        {
            await page.ClickAsync(Config.WhatsAppWebMetadata.AcceptInvalidNumber);
            foreach (var intend in intendeds)
            {
                await StoreNotDeliveredMessage(intend.Message);
            }

            await DeleteNotSents(intendeds);
        }

        private static async Task DeleteNotSents(List<ToBeSent> intendeds)
        {
            using var session = Stores.MegaWhatsAppApi.OpenAsyncSession();
            foreach (var x in intendeds)
            {
                session.Delete(x.Id);
            }

            await session.SaveChangesAsync();
        }
        
        public static void TreatStrangeNumbers(ref string number)
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

            Thread.Sleep(500);
        }
        private static async Task SendHumanizedMessage(Page page, string messageText, string number)
        {
            Humanizer = AutomationQueue.ClientConfiguration.HumanizerConfiguration;
            var random = new Random();
            
            // Greetings
            if (!GetCollaboratorNumbers().Contains(number) || !Humanizer.UseHumanizer)
            {
                await SendGreetings(page, random);
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
                await SendFarewell(page, random);
            }
        }

        private static async Task SendFarewell(Page page, Random random)
        {
            try
            {
                await page.WaitForSelectorAsync(Config.WhatsAppWebMetadata.ChatInput);
                await page.TypeOnElementAsync(
                    elementSelector: Config.WhatsAppWebMetadata.ChatInput,
                    GetHumanizedFarewell(),
                    Humanizer);
                await page.ClickOnElementAsync(Config.WhatsAppWebMetadata.SendMessageButton);
            }
            catch (Exception e)
            {
                DevOpsHelper.StoreFatalErrorAndRestart(e);
            }
        }

        private static async Task SendMessage(Page page, string messageText, Random random, bool sendAfterTyping = true, bool useHumanizer = false)
        {
            try
            {
                await page.WaitForSelectorAsync(Config.WhatsAppWebMetadata.ChatInput);
                Thread.Sleep(TimeSpan.FromSeconds(1));
                await page.ClickOnElementAsync(Config.WhatsAppWebMetadata.ChatInput);

                //await page.ClickAsync(Config.WhatsAppWebMetadata.ChatInput);
                await page.TypeOnElementAsync(
                    elementSelector: Config.WhatsAppWebMetadata.ChatInput,
                    text: GetTextWithRandomSpaceBetweenWords(messageText),
                    Humanizer,
                    useParser: true);
            
                if (sendAfterTyping)
                {
                    await page.ClickOnElementAsync(Config.WhatsAppWebMetadata.SendMessageButton);
                }
            }
            catch (Exception e)
            {
                DevOpsHelper.StoreFatalErrorAndRestart(e);
            }
        }
        
        private static async Task SendGroupOfMessages(Page page, List<string> texts, Random random, bool useHumanizationMessages = false)
        {
            var finalText = string.Empty;

            if (AutomationQueue.ClientConfiguration.ShortenReportMessages && 
                texts.Count(x => x.Contains("O resultado")) >= 2)
            {
                texts = texts.Select(x =>
                    x.Replace("O resultado do paciente", string.Empty)
                     .Replace("O resultado da paciente", string.Empty)
                     .Replace("foi liberado", string.Empty)
                     .Replace(".", string.Empty)
                     .Trim())
                    .ToList();

                texts.Insert(0, "Os resultados dos seguintes pacientes foram liberados:\n");
            }

            foreach (var text in texts.ToImmutableList())
            {
                finalText += text;
                if (text.Contains("\r\n") || text.Contains("\r\n"))
                {
                    finalText += "\n"; // this will send the message.
                    continue;
                }

                finalText += "\r\n";
            }
            
            await SendMessage(page, finalText, random, useHumanizer: useHumanizationMessages);
        }

        private static async Task SendGreetings(Page page, Random random)
        {
            try
            {
                await page.TypeOnElementAsync(
                    Config.WhatsAppWebMetadata.ChatInput,
                    GetHumanizedGreeting(),
                    Humanizer);
                await page.ClickOnElementAsync(Config.WhatsAppWebMetadata.SendMessageButton);
                Thread.Sleep(TimeSpan.FromSeconds(random.Next(Humanizer.MinimumDelayAfterGreeting, Humanizer.MaximumDelayAfterGreeting)));
            }
            catch (Exception e)
            {
                DevOpsHelper.StoreFatalErrorAndRestart(e);
            }
        }
        
        private static string GetClientPresentation()
        {
            return Humanizer.ClientPresentationsPool.Random();
        }
        
        private static string GetHumanizedGreeting()
        {
            return $"{Humanizer.GreetingsPool.Random()} {Humanizer.CumplimentsPool.Random()}\r\n";
        }
        
        private static string GetHumanizedFarewell()
        {
            return Humanizer.FarewellsPool.Random();
        }
        
        private static IList<string> GetCollaboratorNumbers()
        {
            var contactsOnDatabase = Humanizer.CollaboratorsContacts.GetCopy();
            var immutableCount = contactsOnDatabase.Count;
            
            for (int i = 0; i < immutableCount; i++)
            {
                contactsOnDatabase.Add(contactsOnDatabase[i].InsertBrazilian9ThDigit());
                contactsOnDatabase.Add(contactsOnDatabase[i].RemoveBrazilian9ThDigit());
            }

            return contactsOnDatabase.Distinct().ToList();
        }

		public static async Task<bool> CheckIfNumberExists(Page page, string number)
        {
            if (string.IsNullOrEmpty(number) || number.Length < 8)
            {
                return false;
            }

            //// Just to guarantee
            number = number.Trim();
            var doesExist = await CheckIfNumberExistsInternal(page, number);
            if (doesExist)
            {
                return true;
            }
            
            //// Trying the number with and without the brazilian 9th digit
            if (number.ContainsBrazilian9ThDigit())
            {
                var numberWithout9ThDigit = number.RemoveBrazilian9ThDigit();
                number = numberWithout9ThDigit;
                return await CheckIfNumberExistsInternal(page, numberWithout9ThDigit);
            }

            var numberWith9ThDigit = number.InsertBrazilian9ThDigit();
            number = numberWith9ThDigit;
            return await CheckIfNumberExistsInternal(page, numberWith9ThDigit);
        }

        private static async Task<bool> CheckIfNumberExistsInternal(Page page, string number)
        {
            try
            {
                var waitOptions = new WaitForSelectorOptions
                {
                    Visible = true,
                    Timeout = 4000,
                    Hidden = false
                };
                
                await OpenChat(page, number);
                await page.WaitForSelectorAsync(Config.WhatsAppWebMetadata.AcceptInvalidNumber, waitOptions);

                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
        
        public static async Task<bool> CheckPageIntegrity(Page page)
        {
            try
            {
                var waitOptions = new WaitForSelectorOptions
                {
                    Visible = true,
                    Timeout = Convert.ToInt32(TimeSpan.FromSeconds(30).TotalMilliseconds)
                };
                
                await page.WaitForSelectorAsync(Config.WhatsAppWebMetadata.MainPanel, waitOptions);

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

        private static string GetTextWithRandomSpaceBetweenWords(string messageText) 
        {
            var spaceCollection = new[] { " ", "  ", " ", "  ", " ", "  ", " ", "  ", " ", "  "};
            var separated = messageText.Trim().Split(" ")
                .ToImmutableList();

            var final = separated.Aggregate(string.Empty, (current, message) => current + message + spaceCollection.Random());

            return final;
        }
    }
}