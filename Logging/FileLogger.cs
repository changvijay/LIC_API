using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace LIC_WebDeskAPI.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private static readonly object _lock = new object();

        public FileLogger(string filePath)
        {
            _filePath = filePath;
            var logDir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error; // only errors

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {exception?.Message} {formatter(state, exception)} {exception?.StackTrace} \n {new string('_', 50)}";

            lock (_lock)
            {
                File.AppendAllText(_filePath, message + Environment.NewLine);
            }
        }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_filePath);
        }

        public void Dispose() { }
    }
}
