using System.Collections.Generic;
using System.Linq;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class QueryBridge
    {
        public static IList<string> GetValidClientTokens()
        {
            using var session = Stores.MegaWhatsAppApi.OpenSession();
            return session.Query<Client>()
                .Where(x => x.SituationOk)
                .Select(x => x.Token)
                .ToList();
        }
    }
}