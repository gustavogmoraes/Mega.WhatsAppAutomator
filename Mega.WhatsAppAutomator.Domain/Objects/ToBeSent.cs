using System;
using Mega.WhatsAppAutomator.Domain.Interfaces;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class ToBeSent : IRavenDbDocument
    {
        public string Id { get; set; }
        
        public Message Message { get; set; }
        
        public DateTime EntryTime { get; set; }
        
        public bool? CurrentlyProcessingOnAnotherInstance { get; set; }
    }
}