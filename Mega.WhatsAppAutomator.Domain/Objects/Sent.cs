using System;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class Sent
    {
        public string Id { get; set; }
        
        public Message Message { get; set; }
        
        public DateTime EntryTime { get; set; }
        
        public DateTime TimeSent { get; set; }
        
        public TimeSpan DelayToBeSent { get; set; }
    }
}