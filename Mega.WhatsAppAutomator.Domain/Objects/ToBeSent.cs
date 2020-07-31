using System;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class ToBeSent
    {
        public string Id { get; set; }
        
        public Message Message { get; set; }
        
        public DateTime EntryTime { get; set; }
        
        public bool? CurrentlyProcessingOnAnotherInstance { get; set; }
    }
}