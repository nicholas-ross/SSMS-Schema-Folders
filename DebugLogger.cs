using System;
using System.IO;

namespace SsmsSchemaFolders
{
    public static class DebugLogger
    {
        private static readonly string logFilePath = Path.Combine(Path.GetTempPath(), "SsmsSchemaFolders.log");
        private static readonly object _lock = new object();

        static DebugLogger()
        {
            // Clear the log file on the first use in a new session.
            try
            {
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
            catch (Exception)
            {
                // Ignore if we can't delete the file.
            }
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}");
                }
            }
            catch (Exception)
            {
                // Silently fail if logging doesn't work.
            }
        }

        public static void Log(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }
    }
} 