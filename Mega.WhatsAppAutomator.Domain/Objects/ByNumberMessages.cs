using System.Collections.Generic;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class ByNumberMessages
    {
        public string Number { get; set; }
            
        public List<string> Texts { get; set; }
            
        public List<string> IdsToDelete { get; set; }
    }
}