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
        private const string ResultsFolderKey = "UnderGrind.LocomotionTestResultsFolder";
        private static TestRunnerApi _runner;
        private static ResultsCallback _callbacks;
        private static bool _callbacksRegistered;

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string RelativeResultsFolder =>
            EditorPrefs.GetString(ResultsFolderKey, "TestResults/UnderGrind");

        static LocomotionTestResultsRunner()
        {
            // Register globally so runs started from either the Test Runner window or
            // the UnderGrind menu are exported. InitializeOnLoad restores this after
            // the AppDomain reload that can occur during PlayMode tests.
            EditorApplication.delayCall += EnsureCallbacksRegistered;
        }

        [MenuItem("UnderGrind/Tests/Run EditMode Tests + Save Results")]
        public static void RunEditModeTests() => Run(TestMode.EditMode);

        [MenuItem("UnderGrind/Tests/Run PlayMode Tests + Save Results")]
        public static void RunPlayModeTests() => Run(TestMode.PlayMode);

        [MenuItem("UnderGrind/Tests/Choose Results Folder...")]
        public static void ChooseResultsFolder()
        {
            var current = Path.GetFullPath(Path.Combine(ProjectRoot, RelativeResultsFolder));
            var selected = EditorUtility.OpenFolderPanel("Cartella risultati test (dentro il progetto)", current, string.Empty);
            if (string.IsNullOrWhiteSpace(selected))
                return;

            var root = Path.GetFullPath(ProjectRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(selected).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Cartella non valida",
                    "Scegli una cartella interna al progetto Unity. I risultati saranno salvati e consultabili direttamente nel progetto.",
                    "OK");
                return;
            }

            var relative = full.Substring(root.Length).TrimEnd(Path.DirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/');
            EditorPrefs.SetString(ResultsFolderKey, relative);
            Debug.Log("UnderGrind: cartella risultati test impostata su " + relative);
        }

        private static void EnsureCallbacksRegistered()
        {
            if (_callbacksRegistered)
                return;

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
            Debug.Log($"UnderGrind: avviati test {mode}. I risultati saranno salvati automaticamente in: {outputDirectory}");
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
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var prefix = _mode == TestMode.EditMode ? "EditMode" : "PlayMode";
                    var xml = result.ToXml().OuterXml;
                    var timestampedPath = Path.Combine(directory, $"{prefix}_{timestamp}.xml");
                    File.WriteAllText(timestampedPath, xml);
                    File.WriteAllText(Path.Combine(directory, $"{prefix}_Latest.xml"), xml);

                    var summary =
                        $"UnderGrind {_mode} test results{Environment.NewLine}" +
                        $"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                        $"Passed: {result.PassCount}{Environment.NewLine}" +
                        $"Failed: {result.FailCount}{Environment.NewLine}" +
                        $"Skipped: {result.SkipCount}{Environment.NewLine}" +
                        $"Inconclusive: {result.InconclusiveCount}{Environment.NewLine}" +
                        $"Total: {result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount}{Environment.NewLine}" +
                        $"Result: {result.ResultState}{Environment.NewLine}" +
                        $"XML: {timestampedPath}{Environment.NewLine}";
                    File.WriteAllText(Path.Combine(directory, $"{prefix}_{timestamp}.txt"), summary);

                    Debug.Log($"UnderGrind: test {_mode} completati. Passati {result.PassCount}, falliti {result.FailCount}, " +
                              $"saltati {result.SkipCount}. Report: {timestampedPath}");
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