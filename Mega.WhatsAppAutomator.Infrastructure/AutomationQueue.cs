using System;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using PuppeteerSharp;
using Mega.WhatsAppAutomator.Infrastructure.Objects;
using System.Reflection;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.Enums;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.TextNow;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using Raven.Client.Documents;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Raven.Client.Documents.Linq;
using System.Text.RegularExpressions;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using Extensions = Mega.WhatsAppAutomator.Infrastructure.Utils.Extensions;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class AutomationQueue
    {
        private static Page Page { get; set; }
        private static ConcurrentQueue<WhatsAppWebTask> TaskQueue { get; set; }

        public static void AddTask(WhatsAppWebTask task)
        {
            TaskQueue.Enqueue(task);
        }

        public static void StartQueue(Page page)
        {
            Page = page;
            
            TaskQueue ??= new ConcurrentQueue<WhatsAppWebTask>();
            //StopBrowser = false;
            try
            {
                Task.Run(async () => await QueueExecution());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            //Task.Run(async () => await TimeToRestart());
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
                .Take(ClientConfiguration.MessagesPerCycle) // Note, if we use take and save changes on the same session, we remove the objects from the collection
                .ToListAsync();
        }

        private static async Task<List<ToBeSent>> GetMessagesToBeSentAsync()
        {
            var returnList = await GetReturnListAsync();
            if (!returnList.Any() && ClientConfiguration.PriorityzeFinalClients)
            {
                Console.WriteLine("Did not get any final client messages, changing configuration");
                using var session = Stores.MegaWhatsAppApi.OpenAsyncSession();
                var client = await session.Query<Client>()
                    .FirstOrDefaultAsync(x => x.Token == "23ddd2c6-e46c-4030-9b65-ebfc5437d8f1");

                client.SendMessageConfiguration.PriorityzeFinalClients = false;

                await session.SaveChangesAsync();
            }

            foreach (var x in returnList)
            {
                using var session2 = Stores.MegaWhatsAppApi.OpenAsyncSession();
                var tbS = await session2.LoadAsync<ToBeSent>(x.Id);
                tbS.CurrentlyProcessingOnAnotherInstance = true;
                await session2.SaveChangesAsync();
            }

            return returnList;
        }

        private static void GetAndSetClientConfig()
        {
            using var session = Stores.MegaWhatsAppApi.OpenSession();
            ClientConfiguration = session.Query<Client>()
                .Where(x => x.Token == "23ddd2c6-e46c-4030-9b65-ebfc5437d8f1")
                .Select(x => x.SendMessageConfiguration)
                .FirstOrDefault();
        }

        public static SendMessageConfiguration ClientConfiguration { get; set; }
        
        public static bool StopBrowser { get; set; }
        
        private static async Task QueueExecution()
        {
            try
            {
                while (!StopBrowser)
                {
                    //TO DO: After x cycles, clean messages on Whatsapp
                    GetAndSetClientConfig();
                    
                    if (ClientConfiguration.SendMessagesGroupedByNumber)
                    {
                        await SendMessagesGroupingByNumber();
                    }
                    else
                    {
                        await SendMessagesNoStrategy();
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, ClientConfiguration.MaximumDelayBetweenCycles)));
                }

                await Page.Browser.CloseAsync();
                Thread.Sleep(TimeSpan.FromSeconds(3));
                SaveChromeUserData();
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
        }

        private static async Task SendMessagesGroupingByNumber()
        {
            var stp = new Stopwatch();
            stp.Start();
            var groupsOfMessagesByNumber = await GetMessagesToBeSentByGroupAsync();
            stp.Stop();
            
            if (groupsOfMessagesByNumber.Count > 0)
            {
                Console.WriteLine(
                    $"At {DateTime.UtcNow.ToBraziliaDateTime()}, started new cycle of messages grouped by number  {ClientConfiguration.MessagesPerCycleNumberGroupingStrategy} messages, " +
                    $"got {groupsOfMessagesByNumber.Count} groups of messages to be sent, request time: {stp.Elapsed.TimeSpanToReport()}\n");
                await SendGroupOfMessagesByNumber(Page, groupsOfMessagesByNumber);

                return;
            }
            
            Console.WriteLine($"At {DateTime.UtcNow.ToBraziliaDateTime()} Got no messages to sent, idling...");
            Thread.Sleep(TimeSpan.FromSeconds(15));
        }

        private static async Task SendGroupOfMessagesByNumber(Page page, List<IGrouping<string,ToBeSent>> groupsOfMessagesByNumber)
        {
            var outerStopwatch = new Stopwatch();
            outerStopwatch.Start();

            foreach (var group in groupsOfMessagesByNumber)
            {
                while (!(await WhatsAppWebTasks.CheckPageIntegrity(page)))
                {
                    Console.WriteLine("Page integrity compromised, reloading...");
                    await page.ReloadAsync();
                    
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }

                var number = group.Key;
                var texts = group.Select(x => x.Message.Text).ToList();
                var messages = group.ToList();
                
                Console.WriteLine($"At {DateTime.UtcNow.ToBraziliaDateTime()}: Writing {texts.Count} messages to number {number}");
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                
                WhatsAppWebTasks.TreatStrangeNumbers(ref number);
                
                Console.WriteLine("Checking if number exists");
                var numberExists = await WhatsAppWebTasks.CheckIfNumberExists(page, number);
                if (!numberExists)
                {
                    await WhatsAppWebTasks.DismissErrorAndStoreNotDelivereds(page, messages);
                    RemoveNotSentMessages(messages);
                    Console.WriteLine($"Number {number} does not exist on WhatsApp, storing as not delivered");
                }
                else
                {
                    await WhatsAppWebTasks.SendMessageGroupedByNumber(page, number, texts);
                    TickSentMessages(messages);
                    
                    Console.WriteLine($"At {DateTime.UtcNow.ToBraziliaDateTime()}: Sent {texts.Count} messages to number {number}");
                }
                
                stopwatch.Stop();
                Thread.Sleep(new Random().Next(1, ClientConfiguration.MaximumDelayBetweenMessages));
            }
            
            outerStopwatch.Stop();
            Console.WriteLine($"At {DateTime.UtcNow.ToBraziliaDateTime()}, sent group of messages in {outerStopwatch.Elapsed.TimeSpanToReport()}");
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

        private static async Task<List<IGrouping<string, ToBeSent>>> GetMessagesToBeSentByGroupAsync()
        {
            List<ToBeSent> items;
            using (var session = Stores.MegaWhatsAppApi.OpenAsyncSession())
            {
                items = await session.Query<ToBeSent>()
                    .Where(x =>x.CurrentlyProcessingOnAnotherInstance != true)
                    .OrderBy(x => x.EntryTime)
                    .Take(ClientConfiguration.MessagesPerCycleNumberGroupingStrategy)
                    .ToListAsync();
            }

            if (items.Any())
            {
                Stores.MegaWhatsAppApi.BulkUpdate(items, x => x.CurrentlyProcessingOnAnotherInstance, true);
            }
            
            var nmberGrouping = items.GroupBy(x => x.Message.Number).ToList();
            return nmberGrouping;
        }

        private static string GotXMessagesToBeSent() =>
            $"started new cycle of {ClientConfiguration.MessagesPerCycle} messages, " +
            "got {result}.Count messages to be sent, request time: {totalTime}.Count";

        private static async Task SendMessagesNoStrategy()
        {
            var toBeSentMessages = await Extensions.ExecuteWithLogsAsync(GetMessagesToBeSentAsync);
            
            if (toBeSentMessages.Any())
            {
                await SendListOfMessages(Page, toBeSentMessages);
            }
        }

        private static void SaveChromeUserData()
        {
            var browserFilesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserFiles");
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
            
            Console.WriteLine("Compressing file");
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
            
            Console.WriteLine("Saving user data file to database");
            
            using (var session = Stores.MegaWhatsAppApi.OpenSession())
            using (var stream = File.Open(zipPath, FileMode.Open))
            {
                var client = session.Query<Client>().FirstOrDefault(x => x.Token == "23ddd2c6-e46c-4030-9b65-ebfc5437d8f1");
                session.Advanced.Attachments.Store(client, instanceId + ".zip", stream);
                session.SaveChanges();
            }
            
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Console.WriteLine("Saved");
            
            osPlat = DevOpsHelper.GetOsPlatform();
            if (osPlat != OSPlatform.Windows)
            {
                DevOpsHelper.Bash($"chmod 755 {browserFilesDir}");
            }
            else
            {
                new DirectoryInfo(browserFilesDir).GetPermission();
            }
            
            Console.WriteLine("Now trying to delete everything");
            Directory.Delete(browserFilesDir, true);
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
                await WhatsAppWebTasks.SendMessage(page, message.Message);
                if (!ClientConfiguration.HumanizerConfiguration.InsaneMode) { SleepRandomTimeBasedOnConfiguration(); }
                stp.Stop();
                
                Console.WriteLine($"\tAt {DateTime.UtcNow.ToBraziliaDateTime()}, sent a messsage of {message.Message.Text.Length} characters in {stp.Elapsed.TimeSpanToReport()} - {count}/{total}");
            }
            
            TickSentMessages(toBeSentMessages);
            outerStp.Stop();
            Console.WriteLine($"At {DateTime.UtcNow.ToBraziliaDateTime()}, sent {count} messages, on: {outerStp.Elapsed.TimeSpanToReport()}");
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

        private static List<ToBeSent> QueryToBeSentByThisNumber(string number)
        {
            using (var session = Stores.MegaWhatsAppApi.OpenSession())
            {
                return session.Query<ToBeSent>()
                    .Take(1000)
                    .Where(x => x.Message.Number == number)
                    .ToList();
            }
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
            using (var session = Stores.MegaWhatsAppApi.OpenAsyncSession())
            {
                await session.StoreAsync(new ToBeSent
                {
                    Message = message,
                    EntryTime = DateTime.UtcNow.ToBraziliaDateTime()
                });

                await session.SaveChangesAsync();
                
                Console.WriteLine($"Message to {message.Number} delegated via Mega.SmsAutomator");
            }
        }

        // private static async Task TimeToRestart()
        // {
        //     var configuration = GetRestartConfiguration();
        //     var watch = new Stopwatch();
        //     watch.Start();
        //     while (watch.Elapsed < configuration.TimeToRestart)
        //     {
        //     }
        //     StopBrowser = true;
        //     Thread.Sleep(configuration.WaitTime);
        //     Environment.Exit(0);
        // }

        // private static RestartConfiguration GetRestartConfiguration()
        // {
        //     using var session = Stores.MegaWhatsAppApi.OpenSession();
        //     return session.Query<Client>()
        //         .Where(x => x.Token == "23ddd2c6-e46c-4030-9b65-ebfc5437d8f1")
        //         .Select(x => x.RestartConfiguration)
        //         .FirstOrDefault();
        // }
    }
}
