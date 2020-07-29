using System;
using System.IO;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace Mega.WhatsAppAutomator.Infrastructure.TextNow
{
    public static class TextNowTasks
    {
        public static async Task SendMessage(Page page, Message message)
        {
            await page.BringToFrontAsync();
            await page.ClickOnElementAsync("#newText");
            await page.TypeOnElementAsync("#recipientsView > div > div > input", ConvertNumber(message.Number));
            await page.ClickOnElementAsync("#text-input");
            
            //await page.Keyboard.PressAsync("Return");
            
            await page.EvaluateExpressionAsync(BypassExpression(message.Text));
            
            await page.ClickOnElementAsync("#send_button");
            
            Console.WriteLine($"Message sent to {message.Number} via TextNow");
        }

        private static string BypassExpression(string message) =>
            "config.ALLOW_MUTUAL_FORGIVENESS_SMS = true; " +
            "document.querySelector('#send_button').style = 'display:block'; " + 
            $"document.querySelector('#text-input').textContent = `{message}`";
        
        // Works for a time, but as soon as x-csrf-token that goes on header gets refresh
        // we can no longer send messages just by posting on the server
        private static string PostMessageExpression(Message message)
        {
            return
                "fetch(\"https://www.textnow.com/api/users/gustavog.moraes2/messages\", {\n  \"headers\": {\n    \"accept\": \"application/json, text/javascript, */*; q=0.01\",\n    \"accept-language\": \"en-US,en;q=0.9\",\n    \"content-type\": \"application/x-www-form-urlencoded; charset=UTF-8\",\n    \"sec-fetch-dest\": \"empty\",\n    \"sec-fetch-mode\": \"cors\",\n    \"sec-fetch-site\": \"same-origin\",\n    \"x-csrf-token\": \"hK9V5AR7-UkQcJ1zXBlKYPFa-A-VwheBLxzQ\",\n    \"x-requested-with\": \"XMLHttpRequest\"\n  },\n  \"referrer\": \"https://www.textnow.com/messaging\",\n  \"referrerPolicy\": \"no-referrer-when-downgrade\",\n  \"body\": \"json=%7B%22contact_value%22%3A%22%2B{number}%22%2C%22message%22%3A%22{message}%22%2C%22to_name%22%3A%22unknown%22%2C%22message_direction%22%3A2%2C%22message_type%22%3A1%2C%22read%22%3A1%2C%22from_name%22%3A%22Gustavo+Moraes%22%2C%22has_video%22%3Afalse%2C%22new%22%3Atrue%2C%22date%22%3A%222020-07-28T14%3A17%3A53.038Z%22%7D\",\n  \"method\": \"POST\",\n  \"mode\": \"cors\",\n  \"credentials\": \"include\"\n});"
                    .Replace("{number}", message.Number)
                    .Replace("{message}", message.Text);
        }
        
        private static string ConvertNumber(string numeroRecebido)
        {
            var retorno = new string(numeroRecebido);
            if (!retorno.Contains("+55"))
            {
                retorno = "+55" + retorno;
            }

            if (retorno.Length == 13)
            {
                retorno = retorno.Insert(5, "9");
            }

            return retorno;
        }
    }
}