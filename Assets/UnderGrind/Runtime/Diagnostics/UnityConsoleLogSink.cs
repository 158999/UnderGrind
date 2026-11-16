using UnityEngine;

namespace UnderGrind.Diagnostics
{
    public sealed class UnityConsoleLogSink : ILogSink
    {
        public void Write(in LogEvent entry)
        {
            var message = entry.ToString();
            switch (entry.Level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Information:
                    Debug.Log(message);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(message);
                    break;
            }
        }
    }
}
