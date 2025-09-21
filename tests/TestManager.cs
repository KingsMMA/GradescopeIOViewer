namespace GradescopeIOViewer.tests
{
    internal struct TestManager
    {

        private static List<TestInstance> runningTests = new List<TestInstance>();

        public static async void RunTests(string?[] results, string executable, List<string> inputs, List<string> outputs, Action<int> onTestComplete)
        {
            KillTests();
            for (int i = 0; i < results.Length; i++)
            {
                int tempI = i;
                _ = Task.Run(async () => {
                    const int minRuns = 3;
                    const int maxRuns = 10;
                    const double threshold = 0.7;
                    var outputCounts = new Dictionary<string, int>();
                    var outputOrder = new List<string>();
                    int runs = 0;
                    string selectedOutput = null;
                    while (runs < maxRuns)
                    {
                        TestInstance testInstance = await TestInstance.Spawn(executable, inputs[tempI], runningTests);
                        string output = testInstance.output.ToString();
                        runningTests.Remove(testInstance);
                        runs++;
                        if (!outputCounts.ContainsKey(output))
                        {
                            outputCounts[output] = 0;
                            outputOrder.Add(output);
                        }
                        outputCounts[output]++;
                        // Check if any output meets threshold
                        foreach (var kvp in outputCounts)
                        {
                            if (kvp.Value >= minRuns && kvp.Value >= (int)(runs * threshold))
                            {
                                selectedOutput = kvp.Key;
                                break;
                            }
                        }
                        if (selectedOutput != null && runs >= minRuns)
                            break;

                        if (testInstance.WasKilled)
                            break;
                    }
                    // If no output meets threshold, pick most frequent (or first if tied)
                    if (selectedOutput == null)
                    {
                        int maxCount = outputCounts.Values.Max();
                        selectedOutput = outputOrder.First(o => outputCounts[o] == maxCount);
                    }
                    results[tempI] = selectedOutput;
                    onTestComplete(tempI);
                });
            }
        }

        public static void KillTests()
        {
            if (runningTests.Count == 0) return;

            TestInstance[] instances;
            lock (runningTests)
            {
                instances = new TestInstance[runningTests.Count];
                runningTests.CopyTo(instances);
                runningTests.Clear();
            }
            foreach (TestInstance instance in instances)
            {
                instance?.Kill();
            }

            runningTests.Clear();
        }

    }
}
