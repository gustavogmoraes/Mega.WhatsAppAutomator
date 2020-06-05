using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using PuppeteerSharp;
using Mega.WhatsAppAutomator.Infrastructure.Objects;
using System.Reflection;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class AutomationQueue
    {
        private static Page WhatsAppWebPage { get; set; }
        private static ConcurrentQueue<WhatsAppWebTask> TaskQueue { get; set; }

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
                    var methodInfo = typeof(WhatsAppWebTasks)
                        .GetMethods(BindingFlags.Static)
                        .FirstOrDefault(method => method.Name == task.KindOfTask.ToString());

                    if(methodInfo != null)
                    {
                        await ((Task)methodInfo.Invoke(null, new[] { WhatsAppWebPage, task.TaskData })).ConfigureAwait(false);
                    }
                };

                Thread.Sleep(500);
            }
        }
    }
}
