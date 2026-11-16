using System;

namespace UnderGrind.Diagnostics
{
    public sealed class Logger : IUnderGrindLogger
    {
        private readonly object _gate = new object();
        private readonly ILogSink[] _sinks;

        public Logger(LogLevel minimumLevel, params ILogSink[] sinks)
        {
            MinimumLevel = minimumLevel;
            _sinks = sinks == null ? Array.Empty<ILogSink>() : (ILogSink[])sinks.Clone();
        }

        public LogLevel MinimumLevel { get; }

        public bool IsEnabled(LogLevel level) => level >= MinimumLevel && level != LogLevel.None;

        public void Log(LogLevel level, string category, string message, Exception exception = null)
        {
            if (!IsEnabled(level))
                return;

            var entry = new LogEvent(DateTimeOffset.UtcNow, level, category, message, exception);
            lock (_gate)
            {
                for (var i = 0; i < _sinks.Length; i++)
                {
                    var sink = _sinks[i];
                    if (sink == null)
                        continue;

                    try
                    {
                        sink.Write(in entry);
                    }
                    catch (Exception sinkException)
                    {
                        System.Diagnostics.Trace.WriteLine("Logging sink failure: " + sinkException);
                    }
                }
            }
        }

        public void Trace(string category, string message) => Log(LogLevel.Trace, category, message);
        public void Debug(string category, string message) => Log(LogLevel.Debug, category, message);
        public void Information(string category, string message) => Log(LogLevel.Information, category, message);
        public void Warning(string category, string message, Exception exception = null) => Log(LogLevel.Warning, category, message, exception);
        public void Error(string category, string message, Exception exception = null) => Log(LogLevel.Error, category, message, exception);
        public void Critical(string category, string message, Exception exception = null) => Log(LogLevel.Critical, category, message, exception);
    }
}
