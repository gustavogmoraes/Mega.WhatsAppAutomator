using System.Threading;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using PuppeteerSharp;
using Mega.WhatsAppAutomator.Infraestructure.Objects;

namespace Mega.WhatsAppAutomator.Infraestructure
{
    public static class AutomationQueue
    {
        public static void AddTask(WhatsAppWebTask task)
        {
            TaskQueue.Enqueue(task);
        }

        public static void StartQueue(Page page)
        {
            WhatsAppWebPage = page;

            if(TaskQueue == null)
            {
                TaskQueue = new ConcurrentQueue<WhatsAppWebTask>();
            }

            Task.Run(async () => await QueueExecution());
        }

        private static async Task QueueExecution()
        {
            while(true) 
            {
                if(TaskQueue.TryDequeue(out var task))
                {
                    switch(task.KindOfTask)
                    {
                        case "SendMessage":
                            await SendMessage(task.TaskData);
                            break;
                    }
                };

                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        private static async Task SendMessage(dynamic message)
        {
           await WhatsAppWebPage.GoToAsync($@"https://web.whatsapp.com/send?phone={message.Number}&text={message.Text}&source&data&app_absent");
           await WhatsAppWebPage.WaitForNavigationAsync();
           await (await WhatsAppWebPage.QuerySelectorAsync(WhatsAppWebMetadata.SendMessageButton)).ClickAsync();
        }

        private static Page WhatsAppWebPage { get; set; }

        private static ConcurrentQueue<WhatsAppWebTask> TaskQueue { get; set; }
    }
}
