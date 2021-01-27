using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.Persistence;
using Mega.WhatsAppAutomator.Infrastructure.Utils;

namespace Mega.WhatsAppAutomator.Infrastructure.DevOps
{
    public static class DevOpsHelper
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
        
        /// <summary>
        /// Kills a process and all its childs, only for Windows
        /// </summary>
        /// <param name="pid">The pid.</param>
        public static void KillProcessAndChildrenOnWindows(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }
            var searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={pid}");
            var moc = searcher.Get();
            
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildrenOnWindows(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
        
        public static void StoreFatalErrorAndRestart(Exception exception)
        {
            Logger.StoreError(new UntreatedError { Exception = exception });
            
            using var session = Stores.MegaWhatsAppApi.OpenSession();
            var toBeSents = session.Query<ToBeSent>().ToList();
            
            Stores.MegaWhatsAppApi.BulkUpdate(toBeSents, x => x.CurrentlyProcessingOnAnotherInstance, false);
        }
    }
}