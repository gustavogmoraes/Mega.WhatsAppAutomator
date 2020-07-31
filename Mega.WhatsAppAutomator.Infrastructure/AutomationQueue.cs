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

            Task.Run(async () => await QueueExecution());
        }

        private static List<ToBeSent> GetMessagesToBeSent()
        {
            List<ToBeSent> returnList = null;
            GetClientConfig();
            using (var session = Stores.MegaWhatsAppApi.OpenSession())
            {
                returnList = session.Query<ToBeSent>()
                .Where(x => x.CurrentlyProcessingOnAnotherInstance != true)
                .OrderBy(x => x.EntryTime)
                .Take(ClientConfiguration.MessagesPerCycle) // Note, if we use take and save changes on the same session, we remove the objects from the collection
                .ToList();
            }

            //return session.Query<ToBeSent>()
            //    .Search(x => x.Message.Text, "*prefeitura")
            //    .OrderBy(x => x.EntryTime)
            //    .Take(ClientConfiguration.MessagesPerCycle)
            //    .ToList();

            using (var session2 = Stores.MegaWhatsAppApi.OpenSession())
            {
                returnList.ForEach(x =>
                {
                    var tbS = session2.Load<ToBeSent>(x.Id);
                    tbS.CurrentlyProcessingOnAnotherInstance = true;
                });
                session2.SaveChanges();
            }

            return returnList;
        }

        private static void GetClientConfig()
        {
            using var session = Stores.MegaWhatsAppApi.OpenSession();
            ClientConfiguration = session.Query<Client>()
                .Where(x => x.Token == "23ddd2c6-e46c-4030-9b65-ebfc5437d8f1")
                .Select(x => x.SendMessageConfiguration)
                .FirstOrDefault();
        }

        public static SendMessageConfiguration ClientConfiguration { get; set; }

        private static async Task QueueExecution()
        {
            while (true)
            {
                // After x cycles, clean messages
                var stp = new Stopwatch();
                stp.Start();
                var toBeSentMessages = GetMessagesToBeSent();
                stp.Stop();
                
                Console.WriteLine($"At {DateTime.UtcNow.ToBraziliaDateTime()} got {toBeSentMessages.Count} to be sent, request time: {stp.Elapsed.TimeSpanToReport()}");
                
                if (toBeSentMessages.Any())
                {
                    await SendListOfMessages(Page, toBeSentMessages);
                }

                Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, ClientConfiguration.MaximumDelayBetweenCycles)));
            }
        }

        private static async Task SendListOfMessages(Page page, List<ToBeSent> toBeSentMessages)
        {
            var outerStp =  new Stopwatch();
            outerStp.Start();
            foreach (var message in toBeSentMessages)
            {
                await SendMessage(page, message.Message);
                Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(1, ClientConfiguration.MaximumDelayBetweenMessages)));
            }

            var count = toBeSentMessages.Count;
            TickSentMessages(toBeSentMessages);
            outerStp.Stop();
            Console.WriteLine($"At {DateTime.UtcNow.ToBraziliaDateTime()} sent {count} to be sent on: {outerStp.Elapsed.TimeSpanToReport()}");
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
            EntryTime = x.EntryTime, // It's already on Brazilia DateTime
            TimeSent = DateTime.UtcNow.ToBraziliaDateTime()
       };

        private static async Task SendMessage(Page page, Message message)
        {
            var stp = new Stopwatch();
            stp.Start();
            await WhatsAppWebTasks.SendMessage(page, message);
            stp.Stop();
            Console.WriteLine($"At {DateTime.UtcNow.ToBraziliaDateTime()} sent a messsage of {message.Text.Length} characters in {stp.Elapsed.TimeSpanToReport()}");
        }
        
        //private static List<ByNumberMessages> GetListOfByNumberMessages()
        //{
        //    List<ByNumberMessages> listOfByNumberMessages;
        //    using (var session = Stores.MegaWhatsAppApi.OpenSession())
        //    {
        //        var byNumberGrouping = session.Query<ToBeSent>()
        //            .Take(1000)
        //            .GroupBy(toBeSent => toBeSent.Message.Number)
        //            .Select(x => new
        //            {
        //                Number = x.Key,
        //                TextCount = x.Count()
        //            })
        //            .ToList();

        //        listOfByNumberMessages = new List<ByNumberMessages>();
        //        foreach (var group in byNumberGrouping)
        //        {
        //            ProcessAndAddOnListOfMesssagesByNumber(session, @group.Number, listOfByNumberMessages);
        //        }
        //    }

        //    return listOfByNumberMessages;
        //}

        //private static void ProcessAndAddOnListOfMesssagesByNumber(IDocumentSession session, string number, List<ByNumberMessages> listOfByNumberMessages)
        //{
        //    var listOfToBeSents = QueryToBeSentByThisNumber(number);

        //    var texts = new List<string>();
        //    var ids = new List<string>();

        //    foreach (var toBeSent in listOfToBeSents)
        //    {
        //        ids.Add(toBeSent.Id);
        //        texts.Add(toBeSent.Message.Text);
        //    }

        //    listOfByNumberMessages.Add(new ByNumberMessages
        //    {
        //        Number = number,
        //        IdsToDelete = ids.Distinct().ToList(),
        //        Texts = texts.Distinct().ToList()
        //    });
        //}

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
    }
}
