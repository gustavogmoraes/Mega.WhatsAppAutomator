namespace Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport
{
    public static class JavaScriptFunctions
    {
        public const string GetBoudingClientRect = 
            @"(selector) => {
                return document.querySelector(selector).getBoundingClientRect().toJSON();
            }";
        
        public const string CopyMessageToWhatsAppWebTextBox = 
            @"(textBoxSelector, messageText) => { 
                window.InputEvent = window.Event || window.InputEvent;
                var event = new InputEvent('input', {
                    bubbles: true
                });
                
                var textbox = document.querySelector(textBoxSelector);
                textbox.textContent = messageText;
                textbox.dispatchEvent(event);
            }";
    }
}
