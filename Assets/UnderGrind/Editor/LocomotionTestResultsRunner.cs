using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnderGrind.Editor
{
    [InitializeOnLoad]
    public static class LocomotionTestResultsRunner
    {
        private const string RelativeResultsFolder = "TestResults/test/UnderGrind";
        private static TestRunnerApi _runner;
        private static ResultsCallback _callbacks;
        private static bool _callbacksRegistered;

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        static LocomotionTestResultsRunner()
        {
            // Re-register after the AppDomain reload that may occur during PlayMode tests.
            EditorApplication.delayCall += EnsureCallbacksRegistered;
        }

        [MenuItem("UnderGrind/Tests/Run EditMode Tests + Save Results")]
        public static void RunEditModeTests() => Run(TestMode.EditMode);

        [MenuItem("UnderGrind/Tests/Run PlayMode Tests + Save Results")]
        public static void RunPlayModeTests() => Run(TestMode.PlayMode);

        private static void EnsureCallbacksRegistered()
        {
            if (_callbacksRegistered) return;
            _runner ??= ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks ??= new ResultsCallback();
            _runner.RegisterCallbacks(_callbacks);
            _callbacksRegistered = true;
        }

        private static void Run(TestMode mode)
        {
            EnsureCallbacksRegistered();
            var outputDirectory = Path.GetFullPath(Path.Combine(ProjectRoot, RelativeResultsFolder));
            Directory.CreateDirectory(outputDirectory);
            _runner.Execute(new ExecutionSettings(new Filter { testMode = mode }));
            Debug.Log($"UnderGrind: avviati test {mode}. Risultati in: {outputDirectory}");
        }

        private sealed class ResultsCallback : ICallbacks
        {
            private TestMode _mode = TestMode.EditMode;

            public void RunStarted(ITestAdaptor testsToRun) => _mode = testsToRun.TestMode;
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                try
                {
                    var directory = Path.GetFullPath(Path.Combine(ProjectRoot, RelativeResultsFolder));
                    Directory.CreateDirectory(directory);
                    var prefix = _mode == TestMode.EditMode ? "EditMode" : "PlayMode";
                    var xmlPath = Path.Combine(directory, prefix + ".xml");
                    var xml = result.ToXml().OuterXml;
                    File.WriteAllText(xmlPath, xml);

                    var summary =
                        $"UnderGrind {_mode} test results{Environment.NewLine}" +
                        $"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                        $"Passed: {result.PassCount}{Environment.NewLine}" +
                        $"Failed: {result.FailCount}{Environment.NewLine}" +
                        $"Skipped: {result.SkipCount}{Environment.NewLine}" +
                        $"Inconclusive: {result.InconclusiveCount}{Environment.NewLine}" +
                        $"Total: {result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount}{Environment.NewLine}" +
                        $"Result: {result.ResultState}{Environment.NewLine}" +
                        $"XML: {xmlPath}{Environment.NewLine}";
                    File.WriteAllText(Path.Combine(directory, prefix + ".txt"), summary);

                    Debug.Log($"UnderGrind: test {_mode} completati. Passati {result.PassCount}, falliti {result.FailCount}, " +
                              $"saltati {result.SkipCount}. Report: {xmlPath}");
                    EditorApplication.delayCall += () => AssetDatabase.Refresh();
                }
                catch (Exception exception)
                {
                    Debug.LogError("UnderGrind: impossibile salvare i risultati dei test. " + exception);
                }
            }
        }
    }
}