namespace Mega.WhatsAppAutomator.Infrastructure
{
    public class WhatsAppWebMetadata
    {
        public string WhatsAppUrl { get; set; }
        
        public string MainPanel { get; set; }
        
        public string ChatInput { get; set; }
        
        public string SendMessageButton { get; set; }
        
        public string SelectorMainDiv { get; set; }
        
        public string AcceptInvalidNumber { get; set; }
        
        public bool UseCustomUserAgent { get; set; }
        
        public string CustomUserAgent { get; set; }

        public static string SendMessageExpression(string number) =>
            "var link = document.createElement('a');\n" +
           $"link.setAttribute('href', 'whatsapp://send?phone={number}');\n" +
            "document.body.append(link);\n" + 
            "link.click();document.body.removeChild(link);";
        
        private static string WrapSelectorWithQuotes(string selector) => $"'{selector}'";
    }
}