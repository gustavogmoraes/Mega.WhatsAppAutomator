using Mega.WhatsAppAutomator.Domain.Objects;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Mega.WhatsAppAutomator.Api.ApiUtils
{
    public static class ApiExtensions
    {
        /// <summary>
        /// Gets the client session.
        /// </summary>
        /// <returns>The session.</returns>
        public static ClientSession GetClientSession(this HttpContext httpContext) =>
            JsonConvert.DeserializeObject<ClientSession>(httpContext.Session.GetString("clientSession"));
    }
}