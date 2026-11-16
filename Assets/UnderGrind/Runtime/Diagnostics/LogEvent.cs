using System;

namespace UnderGrind.Diagnostics
{
    public readonly struct LogEvent
    {
        public LogEvent(DateTimeOffset timestampUtc, LogLevel level, string category, string message, Exception exception = null)
        {
            TimestampUtc = timestampUtc;
            Level = level;
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
            Message = message ?? string.Empty;
            Exception = exception;
        }

        public DateTimeOffset TimestampUtc { get; }
        public LogLevel Level { get; }
        public string Category { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public override string ToString()
        {
            var line = string.Format("{0:O} [{1}] [{2}] {3}", TimestampUtc, Level, Category, Message);
            return Exception == null ? line : line + Environment.NewLine + Exception;
        }
    }
}
