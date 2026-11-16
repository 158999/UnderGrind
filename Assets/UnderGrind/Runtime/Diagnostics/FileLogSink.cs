using System;
using System.IO;
using System.Text;

namespace UnderGrind.Diagnostics
{
    public sealed class FileLogSink : ILogSink, IDisposable
    {
        private readonly object _gate = new object();
        private readonly StreamWriter _writer;
        private bool _disposed;

        public FileLogSink(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A log file path is required.", nameof(filePath));

            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            _writer = new StreamWriter(new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read), new UTF8Encoding(false))
            {
                AutoFlush = true
            };
        }

        public void Write(in LogEvent entry)
        {
            lock (_gate)
            {
                if (_disposed)
                    return;

                _writer.WriteLine(entry.ToString());
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _writer.Dispose();
            }
        }
    }
}
