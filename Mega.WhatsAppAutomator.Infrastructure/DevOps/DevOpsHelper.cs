using System.Runtime.InteropServices;

namespace Mega.WhatsAppAutomator.Infrastructure.DevOps
{
    public class DevOpsHelper
    {
        public static OSPlatform GetOsPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatform.OSX;
            }

            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) 
                ? OSPlatform.Linux 
                : OSPlatform.Create("Other");
        }
    }
}