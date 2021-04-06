using System.Collections.Generic;

namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class RandomIntPicker
    {
        public string Mode { get; set; }
        
        public int? Min { get; set; }
        
        public int? Max { get; set; }
        
        public List<int> Pool { get; set; }
        
        public List<dynamic> MinMaxPool { get; set; }
    }
}