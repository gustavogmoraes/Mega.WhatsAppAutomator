using System;
using System.IO;

namespace Mega.WhatsAppAutomator.Infrastructure.DevOps
{
    public static class FileManagement
    {
        public static string ScreenshotsDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserFiles", "Screenshots");

        public static FileStream GetLastTakenQrCode()
        {
            return File.OpenRead(Path.Combine(ScreenshotsDirectory, "QrCode.jpg"));
        } 
    }
}
