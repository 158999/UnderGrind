using System;
using System.IO;

namespace UnderGrind.Diagnostics
{
    public sealed class TextWriterLogSink : ILogSink, IDisposable
    {
        private readonly object _gate = new object();
        private readonly TextWriter _writer;
        private readonly bool _ownsWriter;
        private bool _disposed;

        public TextWriterLogSink(TextWriter writer, bool ownsWriter = false)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _ownsWriter = ownsWriter;
        }

        public void Write(in LogEvent entry)
        {
            lock (_gate)
            {
                if (_disposed)
                    return;

                _writer.WriteLine(entry.ToString());
                _writer.Flush();
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                    return;

                _disposed = true;
                if (_ownsWriter)
                    _writer.Dispose();
            }
        }
    }
}
