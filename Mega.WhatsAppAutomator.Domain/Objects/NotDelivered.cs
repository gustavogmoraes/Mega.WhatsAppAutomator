using System;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class NotDelivered
    {
        public string Id { get; set; }    

        public Message Message { get; set; }

        public DateTime ExecutionTime { get; set; }

        public bool AlreadySentBackToClient { get; set; }
    }
}