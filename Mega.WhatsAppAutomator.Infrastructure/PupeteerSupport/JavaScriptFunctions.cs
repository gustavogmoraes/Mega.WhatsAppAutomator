namespace Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport
{
    public class JavaScriptFunctions
    {
        public const string GetBoudingClientRect = 
            @"(selector) => {
                return document.querySelector(selector).getBoundingClientRect().toJSON();
            }";
    }
}
