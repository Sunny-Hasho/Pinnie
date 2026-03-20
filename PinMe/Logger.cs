using System;
using System.IO;
using System.Diagnostics;

namespace Pinnie
{
    public static class Logger
    {
        private static string LogPath;

        static Logger()
        {
            // Use AppData for reliable logging even in single-file published builds
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logDir = Path.Combine(appData, "Pinnie");
            
            try 
            {
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                LogPath = Path.Combine(logDir, "pinnie.log");
            }
            catch 
            {
                // Fallback to local directory if AppData fails
                LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pinnie_fallback.log");
            }
        }

        public static void Log(string message)
        {
            string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}";
            
            // 1. IDE Output (Active Diagnostics)
            Debug.WriteLine(entry);
            
            // 2. Persistent File Output
            try
            {
                File.AppendAllText(LogPath, entry + Environment.NewLine);
            }
            catch { }
        }
    }
}
