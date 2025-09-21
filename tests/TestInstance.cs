using System.Diagnostics;
using System.Text;

namespace GradescopeIOViewer.tests
{

    // Credit to (Shah, 2016) for template - https://stackoverflow.com/a/39624153/12964643
    public class TestInstance
    {

        Process process;
        TaskCompletionSource<string> processExitedTaskSource = new TaskCompletionSource<string>();

        public StringBuilder output = new StringBuilder();
        public bool WasKilled { get; private set; } = false;

        private TestInstance(string executable, string input)
        {
            process = new Process();

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += new DataReceivedEventHandler(process_OutputDataReceived);
            process.ErrorDataReceived += new DataReceivedEventHandler(process_ErrorDataReceived);
            process.Exited += new EventHandler(process_Exited);

            process.StartInfo.FileName = executable;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            process.Start();

            if (!string.IsNullOrEmpty(input))
            {
                string[] lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    process.StandardInput.WriteLine(line);
                }
            }

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }

        public void Kill()
        {
            WasKilled = true;
            process.Kill();
        }

        public static async Task<TestInstance> Spawn(string executable, string input, List<TestInstance> instances)
        {
            TestInstance test = new TestInstance(executable, input);
            instances.Add(test);
            await Task.WhenAny(test.processExitedTaskSource.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            test.process.WaitForExit();
            test.process.WaitForExit(100);
            return test;
        }

        void process_Exited(object? sender, EventArgs e)
        {
            process.WaitForExit();
            process.WaitForExit(100);
            processExitedTaskSource.SetResult(output.ToString());
        }

        void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            output.AppendLine(e.Data);
        }

        void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            output.AppendLine(e.Data);
        }

    }
}
