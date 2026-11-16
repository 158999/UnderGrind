using System;
using System.IO;

namespace UnderGrind.Diagnostics
{
    public sealed class TextWriterLogSink : ILogSink, IDisposable
    {
        private readonly object _gate = new object();
        private readonly TextWriter _writer;
        private readonly bool _ownsWriter;

        public TextWriterLogSink(TextWriter writer, bool ownsWriter = false)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _ownsWriter = ownsWriter;
        }

        public void Write(in LogEvent entry)
        {
            lock (_gate)
            {
                _writer.WriteLine(entry.ToString());
                _writer.Flush();
            }
        }

        public void Dispose()
        {
            if (_ownsWriter)
                _writer.Dispose();
        }
    }
}
