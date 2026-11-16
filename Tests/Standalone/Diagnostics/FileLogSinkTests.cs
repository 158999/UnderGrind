using System;
using System.IO;
using NUnit.Framework;
using UnderGrind.Diagnostics;

namespace UnderGrind.Tests.Diagnostics
{
    [TestFixture]
    public sealed class FileLogSinkTests
    {
        private string _temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            _temporaryDirectory = Path.Combine(Path.GetTempPath(), "UnderGrind.Diagnostics.Tests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_temporaryDirectory))
                Directory.Delete(_temporaryDirectory, true);
        }

        [Test]
        public void FileLogSink_CreatesDirectoryAndPersistsEntries()
        {
            var logPath = Path.Combine(_temporaryDirectory, "nested", "undergrind.log");
            var entry = new LogEvent(DateTimeOffset.UtcNow, LogLevel.Warning, "Persistence", "Persist this entry.");

            using (var sink = new FileLogSink(logPath))
                sink.Write(in entry);

            Assert.That(File.Exists(logPath), Is.True);
            var content = File.ReadAllText(logPath);
            Assert.That(content, Does.Contain("[Warning]"));
            Assert.That(content, Does.Contain("[Persistence]"));
            Assert.That(content, Does.Contain("Persist this entry."));
        }
    }
}
