using System;
namespace Mega.WhatsAppAutomator.Infrastructure.Utils
{
    public static class EnvironmentConfiguration
    {
        public static string DatabaseUrl { get; set; }

        public static string DatabaseName { get; set; }

        public static bool DoesDatabaseNeedsCertificate { get; set; }

        public static bool IsRunningOnHeroku { get; set; }

        public static int LocalPort { get; set; }
    }
}
