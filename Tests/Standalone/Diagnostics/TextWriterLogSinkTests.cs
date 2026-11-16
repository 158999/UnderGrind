using System;
using System.IO;
using NUnit.Framework;
using UnderGrind.Diagnostics;

namespace UnderGrind.Tests.Diagnostics
{
    [TestFixture]
    public sealed class TextWriterLogSinkTests
    {
        [Test]
        public void TextWriterLogSink_WritesFormattedEntries()
        {
            var writer = new StringWriter();
            var entry = new LogEvent(DateTimeOffset.UtcNow, LogLevel.Information, "Console", "Formatted output");

            using (var sink = new TextWriterLogSink(writer))
                sink.Write(in entry);

            var output = writer.ToString();
            Assert.That(output, Does.Contain("[Information]"));
            Assert.That(output, Does.Contain("[Console]"));
            Assert.That(output, Does.Contain("Formatted output"));
        }
    }
}
