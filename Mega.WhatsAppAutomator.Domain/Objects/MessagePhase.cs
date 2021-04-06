using System.Collections.Generic;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class MessagePhase
    {
        public RandomIntPicker SecondsToWaitBefore { get; set; }
        
        public RandomIntPicker TypingDelay { get; set; }
        
        public RandomIntPicker SecondsToWaitAfter { get; set; }
        
        public List<string> Pool { get; set; }
    }
}