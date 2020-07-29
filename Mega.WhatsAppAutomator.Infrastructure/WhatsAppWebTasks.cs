using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using PuppeteerSharp;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class WhatsAppWebTasks
    {
        public static async Task SendMessage(Page page, Message message)
        {
            var openChatExpression = WhatsAppWebMetadata.SendMessageExpression(message.Number);
            
            //Humanize
            await page.EvaluateExpressionAsync(openChatExpression);
            
            await page.TypeOnElementAsync(WhatsAppWebMetadata.ChatContainer, " " + RandomizeText(message.Text));
            
            await page.ClickOnElementAsync(WhatsAppWebMetadata.SendMessageButton);
        }

        private static string RandomizeText(string text)
        {
            return text;
        }
    }
}