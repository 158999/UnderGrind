using System;
using NUnit.Framework;
using UnderGrind.Diagnostics;

namespace UnderGrind.Tests.Diagnostics
{
    [TestFixture]
    public sealed class LogEventTests
    {
        [Test]
        public void LogEvent_PreservesDiagnosticInformation()
        {
            var timestamp = new DateTimeOffset(2026, 11, 16, 10, 30, 0, TimeSpan.Zero);
            var exception = new InvalidOperationException("Original exception");
            var entry = new LogEvent(timestamp, LogLevel.Error, "Player.Movement", "Movement failed", exception);

            Assert.That(entry.TimestampUtc, Is.EqualTo(timestamp));
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(entry.Category, Is.EqualTo("Player.Movement"));
            Assert.That(entry.Message, Is.EqualTo("Movement failed"));
            Assert.That(entry.Exception, Is.SameAs(exception));

            var formatted = entry.ToString();
            Assert.That(formatted, Does.Contain(timestamp.ToString("O")));
            Assert.That(formatted, Does.Contain("[Error]"));
            Assert.That(formatted, Does.Contain("[Player.Movement]"));
            Assert.That(formatted, Does.Contain("Movement failed"));
            Assert.That(formatted, Does.Contain("Original exception"));
        }
    }
}
