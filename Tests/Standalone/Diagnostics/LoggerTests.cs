using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnderGrind.Diagnostics;

namespace UnderGrind.Tests.Diagnostics
{
    [TestFixture]
    public sealed class LoggerTests
    {
        [Test]
        public void Logger_FiltersEntriesBelowMinimumLevel()
        {
            var sink = new RecordingSink();
            var logger = new Logger(LogLevel.Warning, sink);

            logger.Information("Test", "Must be filtered.");
            logger.Warning("Test", "Must be recorded.");

            Assert.That(sink.Entries, Has.Count.EqualTo(1));
            Assert.That(sink.Entries[0].Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(sink.Entries[0].Message, Is.EqualTo("Must be recorded."));
        }

        [Test]
        public void Logger_DispatchesEntriesToAllSinks()
        {
            var first = new RecordingSink();
            var second = new RecordingSink();
            var logger = new Logger(LogLevel.Trace, first, second);

            logger.Information("Test", "Shared entry");

            Assert.That(first.Entries, Has.Count.EqualTo(1));
            Assert.That(second.Entries, Has.Count.EqualTo(1));
            Assert.That(first.Entries[0].Message, Is.EqualTo(second.Entries[0].Message));
            Assert.That(first.Entries[0].TimestampUtc, Is.EqualTo(second.Entries[0].TimestampUtc));
        }

        [Test]
        public void Logger_ContinuesWhenASinkThrows()
        {
            var healthySink = new RecordingSink();
            var logger = new Logger(LogLevel.Trace, new ThrowingSink(), healthySink);

            Assert.DoesNotThrow(() => logger.Error("Test", "Sink failure must be isolated."));

            Assert.That(healthySink.Entries, Has.Count.EqualTo(1));
            Assert.That(healthySink.Entries[0].Level, Is.EqualTo(LogLevel.Error));
        }

        private sealed class RecordingSink : ILogSink
        {
            public List<LogEvent> Entries { get; } = new List<LogEvent>();

            public void Write(in LogEvent entry)
            {
                Entries.Add(entry);
            }
        }

        private sealed class ThrowingSink : ILogSink
        {
            public void Write(in LogEvent entry)
            {
                throw new InvalidOperationException("Expected test sink failure.");
            }
        }
    }
}
