using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;

namespace Mega.WhatsAppAutomator.Infrastructure
{
    public static class Logger
    {
        public static void StoreError(UntreatedError error)
        {
            using(var session = Stores.MegaWhatsAppApi.OpenSession())
            {
                session.Store(error);
                session.SaveChanges();
            }
        }
    }
}