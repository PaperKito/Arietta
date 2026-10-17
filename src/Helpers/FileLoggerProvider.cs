using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Arietta.Helpers;

internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _folder;
    private readonly object _lock = new();

    public FileLoggerProvider(string folder)
    {
        _folder = folder;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(_folder, categoryName, _lock);

    public void Dispose() { }


    private sealed class FileLogger : ILogger
    {
        private readonly string _folder;
        private readonly string _category;
        private readonly object _lock;

        public FileLogger(string folder, string category, object @lock)
        {
            _folder = folder;
            _category = category;
            _lock = @lock;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            try
            {
                string file = Path.Combine(_folder, $"Arietta_{DateTime.Now:yyMMdd}.log");
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] {_category}: {formatter(state, exception)}";
                if (exception is not null)
                {
                    line += Environment.NewLine + exception;
                }
                lock (_lock)
                {
                    Directory.CreateDirectory(_folder);
                    File.AppendAllText(file, line + Environment.NewLine);
                }
            }
            catch { }
        }
    }
}
