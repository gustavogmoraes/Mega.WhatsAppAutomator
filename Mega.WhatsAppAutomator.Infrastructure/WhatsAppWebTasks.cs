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
using Raven.Client.Documents;
using static Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport.PupeteerExtensions;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class WhatsAppWebTasks
    {
        static WhatsAppWebTasks()
        {
            Randomizer = new Random();
        }
        
        private static HumanizerConfiguration Humanizer { get; set; }
        
        private static Random Randomizer { get; set; }
        
        //TODO: Review this method
        public static async Task<bool> SendMessage(Page page, Message message)    
        {
            var (numberExists, triedToOpenChat) = await CheckIfNumberExists(page, message.Number);
            if (!numberExists && !triedToOpenChat)
            {
                await StoreNotDeliveredMessage(message);
                return false;
            }
            
            await page.ClickAsync(Config.WhatsAppWebMetadata.AcceptInvalidNumber);
            await StoreNotDeliveredMessage(message);
            return false;
        }

        public static async Task<bool> SendHumanizedMessageByNumberGroups(Page page, string number, List<string> texts, bool randommicallyDisableHumanization = true)
        {
            Humanizer = AutomationQueue.ClientConfiguration.HumanizerConfiguration;
            var useHumanizationMessages = ShouldUseHumanizationMessages(number);
            
            // Greetings
            if (useHumanizationMessages) { await SendPhase(page, Humanizer.Greeting); }

            var embbededFarewell = false;
            if (useHumanizationMessages && Humanizer.Farewell.EmbbedWithMessage)
            {
                var farewell = GetFromPool(Humanizer.Farewell.Pool);
                var lastText = texts.LastOrDefault();
                var final = lastText + $"\r\n{farewell}";

                texts[^1] = final;
                
                embbededFarewell = true;
            }
            
            // Message
            await SendGroupOfMessages(page, texts, Humanizer.Message, useHumanizationMessages);
            if (useHumanizationMessages)
            {
                var randomWaitTimeAfterMessage = GetRandomWaitTime(Humanizer.Message);
                Thread.Sleep(TimeSpan.FromSeconds(randomWaitTimeAfterMessage));
            }
            
            // Farewell
            if (useHumanizationMessages && !embbededFarewell) { await SendPhase(page, Humanizer.Farewell); }

            return useHumanizationMessages;
        }

        private static bool RandomBoolean()
        {
            return new Random().Next(2) == 0;
        }

        private static bool ShouldUseHumanizationMessages(string number)
        {
            var madeContactPreviously = MadeContactPreviously(number);
            
            return Humanizer.UseHumanizer &&
                   !Humanizer.InsaneMode &&
                   !madeContactPreviously;
        }

        private static bool MadeContactPreviously(string number)
        {
            if (number.ContainsBrazilian9ThDigit())
            {
                number = number.RemoveBrazilian9ThDigit();
            }
            
            var session = Stores.MegaWhatsAppApi.OpenSession();
            return session.Query<Sent>()
                .Search(x => x.Message.Number, $"*{number}*")
                .Any();
        }

        public static async Task DismissErrorAndStoreNotDelivereds(
            Page page, List<ToBeSent> intendeds, bool triedToOpenChat = true)
        {
            if (triedToOpenChat)
            {
                await page.ClickAsync(Config.WhatsAppWebMetadata.AcceptInvalidNumber);
            }
            
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

        private static async Task OpenChat(Page page, string number)
        {
            var openChatExpression = WhatsAppWebMetadata.SendMessageExpression(number);
            await page.EvaluateExpressionAsync(openChatExpression);

            Thread.Sleep(500);
        }
        private static async Task SendHumanizedMessage(Page page, string messageText, string number)
        {
            Humanizer = AutomationQueue.ClientConfiguration.HumanizerConfiguration;
            var useHumanizer = Humanizer != null && Humanizer.UseHumanizer;

            // Greetings
            if (useHumanizer)
            {
                await SendPhase(page, Humanizer.Greeting);
                Thread.Sleep(TimeSpan.FromSeconds(GetRandomWaitTime(Humanizer.Greeting)));
            }
            
            // Message
            await SendMessage(page, messageText, Humanizer?.Message);
            if (useHumanizer)
            {
                Thread.Sleep(TimeSpan.FromSeconds(GetRandomWaitTime(Humanizer.Message)));
            }
            
            // Farewell
            if (useHumanizer)
            {
                await SendPhase(page, Humanizer.Farewell);
                Thread.Sleep(TimeSpan.FromSeconds(GetRandomWaitTime(Humanizer.Farewell)));
            }
        }

        private static async Task SendMessage(Page page, string messageText, MessagePhase phase, bool sendAfterTyping = true, bool useHumanizer = false)
        {
            try
            {
                await page.WaitForSelectorAsync(Config.WhatsAppWebMetadata.ChatInput);
                await page.ClickOnElementAsync(Config.WhatsAppWebMetadata.ChatInput);

                var textToSend = AutomationQueue.ClientConfiguration.HumanizerConfiguration.ScrambleMessageWithWhitespaces
                    ? GetTextWithRandomSpaceBetweenWords(messageText)
                    : messageText;
                
                await page.TypeOnElementAsync(
                    elementSelector: Config.WhatsAppWebMetadata.ChatInput,
                    text: textToSend,
                    phase: phase,
                    useParser: true);
                
                // if (useHumanizer)
                // {
                //     await page.TypeOnElementAsync(
                //         elementSelector: Config.WhatsAppWebMetadata.ChatInput,
                //         text: textToSend,
                //         humanizer: Humanizer,
                //         useParser: true);
                // }
                // else
                // {
                //     Thread.Sleep(TimeSpan.FromMilliseconds(random.Next(600, 3000)));
                //     await page.EvaluateFunctionAsync(
                //         JavaScriptFunctions.CopyMessageToWhatsAppWebTextBox, 
                //         Config.WhatsAppWebMetadata.ChatInput, textToSend);
                // }
                
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
        
        private static async Task SendGroupOfMessages(Page page, List<string> texts, MessagePhase phase, bool useHumanizationMessages = false)
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
                var t = text.Replace("\n", "\r\n");
                finalText += t;
                if (t.Contains("\r\n") || t.Contains("\r\n"))
                {
                    finalText += "\n"; // this will send the message.
                    continue;
                }

                finalText += "\r\n";
            }
            
            await SendMessage(page, finalText, phase, useHumanizer: useHumanizationMessages);
        }

        private static async Task SendPhase(Page page, MessagePhase phase)
        {
            try
            {
                var message = GetFromPool(phase.Pool);
                
                await page.TypeOnElementAsync(Config.WhatsAppWebMetadata.ChatInput, message, phase, useParser: true);
                await page.ClickOnElementAsync(Config.WhatsAppWebMetadata.SendMessageButton);
                
                Thread.Sleep(TimeSpan.FromSeconds(GetRandomWaitTime(phase)));
            }
            catch (Exception e)
            {
                DevOpsHelper.StoreFatalErrorAndRestart(e);
            }
        }

        public static string GetFromPool(IList<string> pool)
        {
            var str = $"{pool.Random()}\r\n";
            
            return Humanizer.ScrambleMessageWithWhitespaces
                ? GetTextWithRandomSpaceBetweenWords(str)
                : str;
        }

        // private static IList<string> GetCollaboratorNumbers()
        // {
        //     var contactsOnDatabase = Humanizer.CollaboratorsContacts.GetCopy();
        //     var immutableCount = contactsOnDatabase.Count;
        //     
        //     for (int i = 0; i < immutableCount; i++)
        //     {
        //         contactsOnDatabase.Add(contactsOnDatabase[i].InsertBrazilian9ThDigit());
        //         contactsOnDatabase.Add(contactsOnDatabase[i].RemoveBrazilian9ThDigit());
        //     }
        //
        //     return contactsOnDatabase.Distinct().ToList();
        // }

		public static async Task<Tuple<bool, bool>> CheckIfNumberExists(Page page, string number)
        {
            if (string.IsNullOrEmpty(number) || number.Length < 8)
            {
                return new Tuple<bool, bool>(false, false);
            }

            //// Just to guarantee
            number = number.Trim();
            var doesExist = await CheckIfNumberExistsInternal(page, number);
            if (doesExist)
            {
                return new Tuple<bool, bool>(true, true);
            }
            
            //// Trying the number with and without the brazilian 9th digit
            if (number.ContainsBrazilian9ThDigit())
            {
                var numberWithout9ThDigit = number.RemoveBrazilian9ThDigit();
                number = numberWithout9ThDigit;
                var exists = await CheckIfNumberExistsInternal(page, numberWithout9ThDigit);
                return new Tuple<bool, bool>(exists, true);
            }

            var numberWith9ThDigit = number.InsertBrazilian9ThDigit();
            number = numberWith9ThDigit;
            var exists2 = await CheckIfNumberExistsInternal(page, numberWith9ThDigit);
            return new Tuple<bool, bool>(exists2, true);
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

        public static string GetTextWithRandomSpaceBetweenWords(string messageText) 
        {
            var spaceCollection = new[] { " ", "  ", " ", "  ", " ", "  ", " ", "  ", " ", "  "};
            var separated = messageText.Trim().Split(" ")
                .ToImmutableList();

            var final = separated.Aggregate(string.Empty, (current, message) => current + message + spaceCollection.Random());

            return final;
        }
    }
}