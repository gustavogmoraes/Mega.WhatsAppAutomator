using System.Diagnostics;
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
        
        public static string Bash(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            var result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}