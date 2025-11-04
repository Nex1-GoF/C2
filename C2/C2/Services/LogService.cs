using System;
using System.Collections.Generic;
using System.IO;
using C2.Models;

namespace C2.Services
{
    internal class LogService
    {
        private readonly List<Log> Logs;
        private readonly string logDirectory;
        private readonly string logFilePath;

        private static LogService _instance;
        public static LogService Instance => _instance ??= new LogService();

        public event Action<Log>? LogAdded;

        private LogService()
        {
            Logs = new List<Log>();

            logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);

            logFilePath = Path.Combine(logDirectory, "log.txt");

            File.WriteAllText(logFilePath, $"[LogService] Started at {DateTime.Now}\n");
        }

        public void AddLog(MessageType type, string message)
        {
            Log newLog = new Log(type, message);
            Logs.Add(newLog);
            WriteToFile(newLog);

            // ✅ 로그 추가 이벤트 발생
            LogAdded?.Invoke(newLog);
        }

        public IReadOnlyList<Log> GetAllLogs() => Logs.AsReadOnly();

        public void Clear()
        {
            Logs.Clear();
            File.WriteAllText(logFilePath, "");
        }

        private void WriteToFile(Log log)
        {
            string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {log.GetLogMessage()}";
            File.AppendAllText(logFilePath, logLine + Environment.NewLine);
        }
    }
}
