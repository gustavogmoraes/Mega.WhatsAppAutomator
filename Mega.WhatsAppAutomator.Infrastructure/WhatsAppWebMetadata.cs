namespace Mega.WhatsAppAutomator.Infrastructure
{
    public class WhatsAppWebMetadata
    {
        public const string WhatsAppURL = "https://web.whatsapp.com/";
        public const string MainPanel = "#pane-side";
        public static string ChatInput = "._1awRl";
        public static string SendMessageButton = "._2Ujuu";
		public static string SelectorMainDiv = "#app > div > div > div.landing-window > div.landing-main";
        public static string AcceptInvalidNumber = "._2XHG4";
        
        public static string SendMessageExpression(string number) =>
            "var link = document.createElement('a');\n" +
           $"link.setAttribute('href', 'whatsapp://send?phone={number}');\n" +
            "document.body.append(link);\n" + 
            "link.click();document.body.removeChild(link);";

        public static string SetChatInput(string text) =>
            $@"document.querySelector({WrapSelectorWithQuotes(ChatInput)}).textContent = '{text}'";

        private static string WrapSelectorWithQuotes(string selector) => $"'{selector}'";
    }
}