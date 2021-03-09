using System.Collections.Generic;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class MessagePhase
    {
        public int MinTypingDelay { get; set; }
        
        public int MaxTypingDelay { get; set; }
        
        public int MinWaitTimeAfter { get; set; }
        
        public int MaxWaitTimeAfter { get; set; }
        
        public IList<string> Pool { get; set; }
    }
}