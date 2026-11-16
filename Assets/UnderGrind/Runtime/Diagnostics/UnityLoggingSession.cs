using System;
using System.IO;
using UnityEngine;

namespace UnderGrind.Diagnostics
{
    public sealed class UnityLoggingSession : IDisposable
    {
        private readonly FileLogSink _fileSink;

        public UnityLoggingSession(LogLevel minimumLevel)
        {
            var logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
            var logPath = Path.Combine(logDirectory, "undergrind.log");
            _fileSink = new FileLogSink(logPath);
            Logger = new Logger(minimumLevel, new UnityConsoleLogSink(), _fileSink);
        }

        public IUnderGrindLogger Logger { get; }

        public void Dispose()
        {
            _fileSink.Dispose();
        }
    }
}
