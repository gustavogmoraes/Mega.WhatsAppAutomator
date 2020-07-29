namespace Mega.WhatsAppAutomator.Domain.Objects
{
    public class SendMessageConfiguration
    {
        public int MessagesPerCycle { get; set; }
        
        public int MaximumDelayBetweenCycles { get; set; }
        
        public HumanizerConfiguration HumanizerConfiguration { get; set; }
        
    }    
}