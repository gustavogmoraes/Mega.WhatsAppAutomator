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
            List<ToBeSent> toBeSentMessages;
            using (var session = Stores.MegaWhatsAppApi.OpenSession())
            {
                toBeSentMessages = session.Query<ToBeSent>()
                    .OrderBy(x => x.EntryTime)
                    .Take(50)
                    .ToList();
            }

            return toBeSentMessages;
        }

        private static async Task QueueExecution()
        {
            while (true)
            {
                var toBeSentMessages = GetMessagesToBeSent();
                if (toBeSentMessages.Any())
                {
                    await SendListOfMessages(Page, toBeSentMessages);
                }
                //if(TaskQueue.TryDequeue(out var task))
                //{
                //    Console.WriteLine($"Found task: message to {((Message)task.TaskData).Number}");

                //    var methodInfo = typeof(WhatsAppWebTasks)
                //        .GetMethods()
                //        .FirstOrDefault(method => method.Name == task.KindOfTask.ToString());

                //    if(methodInfo != null)
                //    {
                //        Console.WriteLine($"Executing ...");
                //        await ((Task)methodInfo.Invoke(null, new[] { Page, task.TaskData })).ConfigureAwait(false);
                //        Console.WriteLine($"Done");
                //    }
                //};

                Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(2, 28)));
            }
        }

        private static async Task SendListOfMessages(Page page, List<ToBeSent> toBeSentMessages)
        {
            foreach (var message in toBeSentMessages)
            {
                await SendMessage(page, message.Message);
                Thread.Sleep(TimeSpan.FromSeconds(2));
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
