using System;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using PuppeteerSharp;
using Mega.WhatsAppAutomator.Infrastructure.Objects;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using Raven.Client.Documents;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Raven.Client.Documents.Linq;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using static Mega.WhatsAppAutomator.Infrastructure.Utils.Extensions;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class AutomationQueue
    {
        private static Page Page { get; set; }
        private static ConcurrentQueue<WhatsAppWebTask> TaskQueue { get; set; }

        public static void AddTask(WhatsAppWebTask task) => TaskQueue.Enqueue(task);

        public static void StartQueue(Page page)
        {
            ReadyToBeShutdown = false;
            Page = page;
            TotalIdleTime = new TimeSpan();
            
            TaskQueue ??= new ConcurrentQueue<WhatsAppWebTask>();

            try
            { 
                Task.Run(async () => await QueueExecution());
            }
            catch (Exception e)
            {
                WriteOnConsole(e.Message);
                throw;
            }
        }

        private static async Task<List<ToBeSent>> GetReturnListAsync()
        {
            using var session = Stores.MegaWhatsAppApi.OpenAsyncSession();

            if (ClientConfiguration.PriorityzeFinalClients)
            {
                return await session.Query<ToBeSent>()
                    .Search(x => x.Message.Text, "*prefeitura")
                    .OrderBy(x => x.EntryTime)
                    .Take(ClientConfiguration.MessagesPerCycle)
                    .ToListAsync();
            }
            
            return await session.Query<ToBeSent>()
                .Where(x => x.CurrentlyProcessingOnAnotherInstance != true)
                .OrderBy(x => x.EntryTime)
                .Take(ClientConfiguration.MessagesPerCycle)
                //// Important note, if we use take and save changes on the same session, we remove the objects from the collection
                .ToListAsync();
        }

        private static async Task<List<ToBeSent>> GetMessagesToBeSentAsync()
        {
            var returnList = await GetReturnListAsync();
            if (!returnList.Any() && ClientConfiguration.PriorityzeFinalClients)
            {
                WriteOnConsole("Did not get any final client messages, changing configuration");
                using var session = Stores.MegaWhatsAppApi.OpenAsyncSession();
                var client = await session.Query<Client>()
                    .FirstOrDefaultAsync(x => x.Id == EnvironmentConfiguration.ClientId);

                client.SendMessageConfiguration.PriorityzeFinalClients = false;

                await session.SaveChangesAsync();
            }

            foreach (var x in returnList)
            {
                using var session = Stores.MegaWhatsAppApi.OpenAsyncSession();
                var toBeSent = await session.LoadAsync<ToBeSent>(x.Id);
                toBeSent.CurrentlyProcessingOnAnotherInstance = true;

                await session.SaveChangesAsync();
            }

            return returnList;
        }

        private static void GetAndSetClientConfig()
        {
            var success = false;
            while (!success)
            {
                try
                {
                    using var session = Stores.MegaWhatsAppApi.OpenSession();
                    ClientConfiguration = session.Query<Client>()
                        .Where(x => x.Id == EnvironmentConfiguration.ClientId)
                        .Select(x => x.SendMessageConfiguration)
                        .FirstOrDefault();

                    success = true;
                }
                catch (Exception)
                {
                    WriteOnConsole("Failed, trying again");
                }
            }
        }

        public static SendMessageConfiguration ClientConfiguration { get; set; }
        
        public static bool StopBrowser { get; set; }

        private static Stopwatch Stopwatch { get; set; }

        private static void EvaluatePauses()
        {
            // while (true)
            // {
            //     // TODO 
            //     
            //     Thread.Sleep(TimeSpan.FromSeconds(1));
            // }
        }
        
        private static async Task QueueExecution()
        {
            try
            {
                Stopwatch = new Stopwatch();
                Stopwatch.Start();

                _ = Task.Run(EvaluatePauses);
                
                while (!StopBrowser)
                {
                    //TODO: After x cycles, clean messages on Whatsapp
                    GetAndSetClientConfig();
                    
                    // if (ClientConfiguration.SendMessagesGroupedByNumber)
                    // {
                    //     await SendMessagesGroupingByNumber();
                    // }
                    // else
                    // {
                    //     await SendMessagesNoStrategy();
                    // }

                    Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, ClientConfiguration.MaximumDelayBetweenCycles)));
                }

                await Page.Browser.CloseAsync();
                Thread.Sleep(TimeSpan.FromSeconds(3));
                SaveChromeUserData();
                ReadyToBeShutdown = true;
            }
            catch (Exception e)
            {
                WriteOnConsole(e.Message);
                throw;
            }
        }

        private static async Task <int> GetToBeSentCount()
        {
            using var session = Stores.MegaWhatsAppApi.OpenAsyncSession();
            return await session.Query<ToBeSent>()
                .Where(x => x.CurrentlyProcessingOnAnotherInstance != true)
                .CountAsync();
        }
        
        private static TimeSpan TotalIdleTime { get; set; }
            
        private static async Task SendMessagesGroupingByNumber()
        {
            var (groupsOfMessagesByNumber, totalOfGottenMessages) = 
                await ExecuteWithElapsedTime(async () => 
                    await GetMessagesToBeSentByGroupAsync(), out var queryTime);
            var toBeSentCount = await GetToBeSentCount();

            if (groupsOfMessagesByNumber.Count > 0)
            {
                TotalIdleTime = new TimeSpan();
                LastWrittenLine = null;
                WriteOnConsole(GetReportMessage(groupsOfMessagesByNumber, toBeSentCount, totalOfGottenMessages, queryTime));
                
                await SendGroupOfMessagesByNumber(Page, groupsOfMessagesByNumber);

                return;
            }

            Idle();
        }

        private static void Idle()
        {
            if (!string.IsNullOrEmpty(LastWrittenLine) && LastWrittenLine.Contains("Got no messages to send"))
            {
                //ClearCurrentConsoleLine();
                WriteOnConsole(GetIdlingReportLine());
                
                TotalIdleTime = TotalIdleTime.Add(TimeSpan.FromSeconds(ClientConfiguration.IdleTime));
                LastTimeThatIdled = DateTime.UtcNow.ToBraziliaDateTime();
                Thread.Sleep(TimeSpan.FromSeconds(ClientConfiguration.IdleTime));

                return;
            }
            
            WriteOnConsole($"{DateTime.UtcNow.ToBraziliaDateTime()} Got no messages to send, idling for {ClientConfiguration.IdleTime} seconds...");

            LastTimeThatIdled = DateTime.UtcNow.ToBraziliaDateTime();
            
            Thread.Sleep(TimeSpan.FromSeconds(ClientConfiguration.IdleTime));
        }

        private static void ClearConsole()
        {
            Console.Clear();
        }

        private static string GetIdlingReportLine()
        {
            var currentTime = DateTime.UtcNow.ToBraziliaDateTime().RemoveDateConvertingToString();

            ClearConsole();
            
            return $"{LastTimeThatIdled} ˜ {currentTime} " +
                   $"Got no messages to send, idling for {ClientConfiguration.IdleTime} seconds... " +
                   $"Total time idling: {TotalIdleTime.TimeSpanToReport()}";
        }

        private static string GetReportMessage(List<IGrouping<string, ToBeSent>> groupsOfMessagesByNumber, int toBeSentCount, int totalOfMessages, TimeSpan queryTime)
        {
            return $"{DateTime.UtcNow.ToBraziliaDateTime()}, started new cycle of messages grouped by number\n" +
                   $"Group size: {ClientConfiguration.MessagesPerCycleNumberGroupingStrategy}\n" +
                   $"Number of gotten messages: {totalOfMessages}\n" +
                   $"Total of groups: {groupsOfMessagesByNumber.Count}\n" +
                   $"Number of messages remaining to be sent: {toBeSentCount}\n" +
                   $"Request time: {queryTime.TimeSpanToReport(true)}";
        }

        private static async Task SendGroupOfMessagesByNumber(Page page, List<IGrouping<string,ToBeSent>> groupsOfMessagesByNumber)
        {
            var outerStopwatch = new Stopwatch();
            outerStopwatch.Start();

            foreach (var group in groupsOfMessagesByNumber)
            {
                while (!await WhatsAppWebTasks.CheckPageIntegrity(page))
                {
                    WriteOnConsole("\tPage integrity compromised, reloading...");
                    await page.ReloadAsync();
                    
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }
                
                GetAndSetClientConfig();

                var number = group.Key;
                var texts = group.Select(x => x.Message.Text).Distinct().ToList();
                var messages = group.ToList();
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                //// During the number exists JS expression evaluation, if number exists the chat page with the number is already opened
                var (numberExists, triedToOpenChat) = await WhatsAppWebTasks.CheckIfNumberExists(page, number);
                if (!numberExists)
                {
                    await WhatsAppWebTasks.DismissErrorAndStoreNotDelivereds(page, messages, triedToOpenChat);
                    RemoveNotSentMessages(messages);
                    WriteOnConsole($"\t{DateTime.UtcNow.ToBraziliaDateTime()}: " +
                                   $"Number {number.NumberToReport()} does not exist on WhatsApp, storing as not delivered");
                }
                else
                {
                    var usedHumanizationMessages = await WhatsAppWebTasks.SendHumanizedMessageByNumberGroups(page, number, texts);
                    TickSentMessages(messages);

                    var totalLength = texts.Sum(x =>x.Length);
                    stopwatch.Stop();
                    WriteOnConsole(
                        $"\t{DateTime.UtcNow.ToBraziliaDateTime()}: " +
                        $"Sent {texts.Count, 3} messages with total length of {totalLength, 5} characters to number " +
                        $"{number.NumberToReport()} in {stopwatch.Elapsed.TimeSpanToReport()} {(usedHumanizationMessages ? "(Used humanization messages)" : string.Empty)}");
                }

                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                }

                Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, ClientConfiguration.MaximumDelayBetweenMessages)));
            }
            
            outerStopwatch.Stop();
            WriteOnConsole($"{DateTime.UtcNow.ToBraziliaDateTime()}, sent group of {groupsOfMessagesByNumber.Count} messages in {outerStopwatch.Elapsed.TimeSpanToReport()}");
        }

        private static void RemoveNotSentMessages(List<ToBeSent> messages)
        {
            using var session = Stores.MegaWhatsAppApi.OpenSession();
            foreach (var x in messages)
            {
                session.Delete(x.Id);
            }

            session.SaveChanges();
        }

        private static async Task<Tuple<List<IGrouping<string, ToBeSent>>, int>> GetMessagesToBeSentByGroupAsync()
        {
            using var session = Stores.MegaWhatsAppApi.OpenAsyncSession();
            var items = await session.Query<ToBeSent>()
                .Where(x => x.CurrentlyProcessingOnAnotherInstance != true)
                .OrderBy(x => x.EntryTime)
                .Take(ClientConfiguration.MessagesPerCycleNumberGroupingStrategy)
                .ToListAsync();

            if (items.Any())
            {
                Stores.MegaWhatsAppApi.BulkUpdate(items, x => x.CurrentlyProcessingOnAnotherInstance, true);
            }
            
            var numberGrouping = items.GroupBy(x => x.Message.Number).ToList();
            return new Tuple<List<IGrouping<string, ToBeSent>>, int>(numberGrouping, items.Count);
        }

        private static string GotXMessagesToBeSent() =>
            $"Started new cycle of {ClientConfiguration.MessagesPerCycle} messages, " +
             "got {result}.Count messages to be sent, request time: {totalTime}.Count";

        private static async Task SendMessagesNoStrategy()
        {
            var toBeSentMessages = await ExecuteWithLogsAsync(GetMessagesToBeSentAsync);
            
            if (toBeSentMessages.Any())
            {
                await SendListOfMessages(Page, toBeSentMessages);
            }
        }

        private static void SaveChromeUserData()
        {
            var browserFilesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "BrowserFiles");
            var userDataDirPath = Path.Combine(browserFilesDir, "user-data-dir");

            var instanceId = EnvironmentConfiguration.InstanceId;
            var zipPath = Path.Combine(browserFilesDir, instanceId + ".zip");

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetPermission();
            
            new DirectoryInfo(Path.Combine(PupeteerMetadata.UserDataDir, "Default")).DeleteAllBut(PupeteerMetadata.UserDataDirDirectoriesAndFilesExceptionsToNotDelete);
            new DirectoryInfo(PupeteerMetadata.UserDataDir).DeleteAllBut(new[] { Path.Combine(PupeteerMetadata.UserDataDir, "Default") });
            
            WriteOnConsole("Compressing file");
            ZipFile.CreateFromDirectory(userDataDirPath, zipPath, CompressionLevel.Optimal, false);
            
            Thread.Sleep(TimeSpan.FromSeconds(5));
            var osPlat = DevOpsHelper.GetOsPlatform();
            if (osPlat != OSPlatform.Windows)
            {
                DevOpsHelper.Bash($"chmod 755 {browserFilesDir}");
            }
            else
            {
                new DirectoryInfo(browserFilesDir).GetPermission();
            }
            
            WriteOnConsole("Saving user data file to database");
            
            using (var session = Stores.MegaWhatsAppApi.OpenSession())
            using (var stream = File.Open(zipPath, FileMode.Open))
            {
                var client = session.Query<Client>().FirstOrDefault(x => x.Id == EnvironmentConfiguration.ClientId);
                session.Advanced.Attachments.Store(client, instanceId + ".zip", stream);
                
                session.SaveChanges();
            }
            
            Thread.Sleep(TimeSpan.FromSeconds(5));
            WriteOnConsole("Saved");
            
            osPlat = DevOpsHelper.GetOsPlatform();
            if (osPlat != OSPlatform.Windows)
            {
                DevOpsHelper.Bash($"chmod 755 {browserFilesDir}");
            }
            else
            {
                new DirectoryInfo(browserFilesDir).GetPermission();
            }
            
            WriteOnConsole("Now trying to delete browser files");
            Directory.Delete(browserFilesDir, true);
            WriteOnConsole("Deleted");
        }

        private static async Task SendListOfMessages(Page page, List<ToBeSent> toBeSentMessages)
        {
            var outerStp =  new Stopwatch();
            outerStp.Start();
            var count = 0;
            var total = toBeSentMessages.Count;
            foreach (var message in toBeSentMessages)
            {
                count++;
                var stp = new Stopwatch();
                stp.Start();
                var didSend = await WhatsAppWebTasks.SendMessage(page, message.Message);
                if (!didSend)
                {
                    stp.Stop();
                    continue;
                }
                
                if (!ClientConfiguration.HumanizerConfiguration.InsaneMode) { SleepRandomTimeBasedOnConfiguration(); }
                stp.Stop();
                
                WriteOnConsole($"\tAt {DateTime.UtcNow.ToBraziliaDateTime()}, sent a messsage of {message.Message.Text.Length} characters in {stp.Elapsed.TimeSpanToReport()} - {count}/{total}");
            }
            
            TickSentMessages(toBeSentMessages);
            outerStp.Stop();
            WriteOnConsole($"At {DateTime.UtcNow.ToBraziliaDateTime()}, sent {count} messages, on: {outerStp.Elapsed.TimeSpanToReport()}");
        }

        private static void SleepRandomTimeBasedOnConfiguration()
        {
            Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, ClientConfiguration.MaximumDelayBetweenMessages)));
        }

        private static void TickSentMessages(List<ToBeSent> sentMessages)
        {
            using var session = Stores.MegaWhatsAppApi.OpenSession();
            foreach (var x in sentMessages)
            {
                session.Delete(x.Id);
            }

            var listOfSents = sentMessages.Select(SentMessageSelector).ToList();
            foreach (var x in listOfSents)
            {
                x.DelayToBeSent = x.TimeSent.Subtract(x.EntryTime); 
                session.Store(x);
            }
            
            session.SaveChanges();
        }

        private static Func<ToBeSent, Sent> SentMessageSelector =>x => new Sent
        {
            Message = x.Message,
            EntryTime = x.EntryTime,
            TimeSent = DateTime.UtcNow.ToBraziliaDateTime()
        };

        public static bool ReadyToBeShutdown { get; set; }

        private static List<ToBeSent> QueryToBeSentByThisNumber(string number)
        {
            using var session = Stores.MegaWhatsAppApi.OpenSession();
            return session.Query<ToBeSent>()
                .Take(1000)
                .Where(x => x.Message.Number == number)
                .ToList();
        }

        private static List<Message> TreatLongMessage(Message message)
        {
            var listOfMessages = new List<Message>();
            if (message.Text.Length > 150)
            {
                var text = message.Text;
                var chunks = text.SplitOnChunks(150);

                foreach (var chunk in chunks)
                {
                    listOfMessages.Add(new Message
                    {
                        Number = message.Number,
                        Text = chunk
                    });
                }
            }
            else
            {
                listOfMessages.Add(message);
            }

            return listOfMessages;
        }

        private static async Task DelegateSendMessageToMobilePhone(Message message)
        {
            using var session = Stores.MegaWhatsAppApi.OpenAsyncSession();
            await session.StoreAsync(new ToBeSent
            {
                Message = message,
                EntryTime = DateTime.UtcNow.ToBraziliaDateTime()
            });

            await session.SaveChangesAsync();
                
            WriteOnConsole($"Message to {message.Number} delegated via Mega.SmsAutomator");
        }
    }
}
