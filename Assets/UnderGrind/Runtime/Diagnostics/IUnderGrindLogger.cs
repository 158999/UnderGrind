using System;

namespace UnderGrind.Diagnostics
{
    public interface IUnderGrindLogger
    {
        bool IsEnabled(LogLevel level);
        void Log(LogLevel level, string category, string message, Exception exception = null);
        void Trace(string category, string message);
        void Debug(string category, string message);
        void Information(string category, string message);
        void Warning(string category, string message, Exception exception = null);
        void Error(string category, string message, Exception exception = null);
        void Critical(string category, string message, Exception exception = null);
    }
}
