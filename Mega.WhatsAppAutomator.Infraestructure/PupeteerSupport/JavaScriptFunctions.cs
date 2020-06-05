using System;

namespace Mega.WhatsAppAutomator.Infraestructure.PupeteerSupport
{
    public class JavaScriptFunctions
    {
        public const string GetBoudingClientRect = 
            @"(selector) => {
                return document.querySelector(selector).getBoundingClientRect().toJSON();
            }";
    }
}
