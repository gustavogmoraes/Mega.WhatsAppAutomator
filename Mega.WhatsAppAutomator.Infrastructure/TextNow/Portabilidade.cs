using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Infrastructure.Enums;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using PuppeteerSharp;
using PuppeteerSharp.Contrib.Extensions;

namespace Mega.WhatsAppAutomator.Infrastructure.TextNow
{
    public static class Portabilidade
    {
        public static Page Page { get; set; }

        public static async Task Start(Browser browser)
        {
            Page = await browser.NewPageAsync();

            await Page.GoToAsync("https://qualoperadora.info/");
            //await Page.GoToAsync("https://consultaoperadora.com.br/site2015/");
        }

        public static async Task<Carrier?> GetCarrier(string number)
        {
            await Page.BringToFrontAsync();
            await Page.GoToAsync("https://qualoperadora.info/");
            await Page.TypeOnElementAsync("#tel", number);
            await Page.ClickOnElementAsync("#bto");
            
            var resultSelector = "#ctd > div.resultado > div > img";

            await Page.WaitForSelectorAsync(resultSelector);
            var operadora = await Page.QuerySelectorAsync(resultSelector);

            return operadora == null 
                ? null 
                : TreatCarrier(await operadora.GetAttributeAsync("title"));
        }

        private static Carrier? TreatCarrier(string name)
        {
            return name.ToLowerInvariant() == "tim" ? Carrier.Tim : (Carrier?) null;
        }
    }
}