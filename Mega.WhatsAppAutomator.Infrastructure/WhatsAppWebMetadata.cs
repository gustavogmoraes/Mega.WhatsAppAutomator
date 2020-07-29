namespace Mega.WhatsAppAutomator.Infrastructure
{
    public class WhatsAppWebMetadata
    {
        public const string WhatsAppURL = "https://web.whatsapp.com/";
        public const string MainPanel = "#pane-side";
        public const string SearchInput = ".jN-F5";
        public const string PersonItem = "._2wP_Y";
        public const string MessageLine = "vW7d1";
        public static string ChatContainer = "._2FbwG";
        public static string ChatInput = "._2S1VP";
        public static string SendMessageButton = "._1U1xa";
		public static string SelectorMainDiv = "#app > div > div > div.landing-window > div.landing-main";
        public static string Unread = "._31gEB";
        public static string SelectorChatInput = "#main > footer > div._3ee1T._1LkpH.copyable-area > div._3uMse > div > div._3FRCZ.copyable-text.selectable-text";

        public static string SendMessageExpression(string number) =>
            "var link = document.createElement('a');\n" +
           $"link.setAttribute('href', 'whatsapp://send?phone={number}');\n" +
            "document.body.append(link);\n" + 
            "link.click();document.body.removeChild(link);";

        public static string SetChatInput(string text) =>
            $@"document.querySelector({WrapSelectorWithQuotes(SelectorChatInput)}).textContent = '{text}'";

        private static string WrapSelectorWithQuotes(string selector) => $"'{selector}'";
    }
}