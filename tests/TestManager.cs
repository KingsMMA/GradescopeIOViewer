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
                    TestInstance testInstance = await TestInstance.Spawn(executable, inputs[tempI], runningTests);
                    string output = testInstance.output.ToString();
                    results[tempI] = output;
                    runningTests.Remove(testInstance);
                    onTestComplete(tempI);
                });
            }
        }

        public static void KillTests()
        {
            if (runningTests.Count == 0) return;

            foreach (TestInstance instance in runningTests)
            {
                instance?.Kill();
            }

            runningTests.Clear();
        }

    }
}
