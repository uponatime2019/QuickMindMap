using System;
using System.IO;
using System.Text;

namespace QuickMindMap.Helpers
{
    public static class AppLogger
    {
        private static readonly object _lock = new object();
        private static string? _logFilePath;

        public static void Initialize()
        {
            try
            {
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QuickMindMap",
                    "logs");

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string sessionTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _logFilePath = Path.Combine(logDirectory, $"app_session_{sessionTimestamp}.txt");

                LogInfo("AppLogger initialized successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppLogger Init Error] {ex}");
            }
        }

        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public static void LogError(string message, Exception? ex = null)
        {
            string formatted = ex != null ? $"{message} | Exception: {ex}" : message;
            WriteLog("ERROR", formatted);
        }

        public static void LogException(Exception ex, string context = "")
        {
            string msg = string.IsNullOrEmpty(context) ? ex.ToString() : $"[{context}] {ex}";
            WriteLog("FATAL", msg);
        }

        private static void WriteLog(string level, string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            System.Diagnostics.Debug.WriteLine(line);

            if (string.IsNullOrEmpty(_logFilePath)) return;

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Avoid crashing on logging failure
                }
            }
        }
    }
}
