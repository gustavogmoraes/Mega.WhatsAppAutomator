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
            using (var session = Stores.MegaWhatsAppApi.OpenSession())
            {
                ClientConfiguration = session.Query<Client>()
                    .Where(x => x.Token == "23ddd2c6-e46c-4030-9b65-ebfc5437d8f1")
                    .Select(x => x.SendMessageConfiguration)
                    .FirstOrDefault();

                //return session.Query<ToBeSent>()
                //    .Search(x => x.Message.Text, "*prefeitura")
                //    .OrderBy(x => x.EntryTime)
                //    .Take(ClientConfiguration.MessagesPerCycle)
                //    .ToList();


                return session.Query<ToBeSent>()
                    .OrderBy(x => x.EntryTime)
                    .Take(ClientConfiguration.MessagesPerCycle)
                    .ToList();
            }
        }

        public static SendMessageConfiguration ClientConfiguration { get; set; }

        private static async Task QueueExecution()
        {
            while (true)
            {
                var stp = new Stopwatch();
                var toBeSentMessages = GetMessagesToBeSent();
                stp.Stop();
                if (toBeSentMessages.Any())
                {
                    await SendListOfMessages(Page, toBeSentMessages);
                }

                Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(ClientConfiguration.MaximumDelayBetweenCycles)));
            }
        }

        private static async Task SendListOfMessages(Page page, List<ToBeSent> toBeSentMessages)
        {
            foreach (var message in toBeSentMessages)
            {
                await SendMessage(page, message.Message);
                //Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            TickSentMessages(toBeSentMessages);
        }    

        private static void TickSentMessages(List<ToBeSent> sentMessages)
        {
            using(var session = Stores.MegaWhatsAppApi.OpenSession())
            {
                sentMessages.ForEach(x => session.Delete(x.Id));
                sentMessages.Select(x => new Sent
                {
                    Message = x.Message,
                    TimeSent = DateTime.UtcNow
                }).ToList().ForEach(x => session.Store(x));
                session.SaveChanges();
            }
        }

        private static async Task SendMessage(Page page, Message message)
        {
            await WhatsAppWebTasks.SendMessage(page, message);
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
