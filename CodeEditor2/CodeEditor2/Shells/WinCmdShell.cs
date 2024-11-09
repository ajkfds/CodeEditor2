using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Shells
{
    public class WinCmdShell : Shell
    {

        public WinCmdShell(List<string> commands)
        {
            string? cmd_exe = System.Environment.GetEnvironmentVariable("ComSpec");
            if (cmd_exe == null) throw new Exception();

            initialize(
                cmd_exe,   // cmd.exe
                "/K \"chcp 65001\"", // utf-8
                commands
                );
        }

        public WinCmdShell()
        {
            string? cmd_exe = System.Environment.GetEnvironmentVariable("ComSpec");
            if (cmd_exe == null) throw new Exception();

            initialize(
                cmd_exe,   // cmd.exe
                "/K \"chcp 65001\"", // utf-8
                new List<string> { }
                );
        }

        public WinCmdShell(string command, string arguments)
        {
            initialize(
                command,
                arguments,
                new List<string> { }
                );
        }

        public WinCmdShell(string command, string arguments, List<string> commands)
        {
            initialize(
                command,
                arguments,
                commands
                );
        }

        public override event ReceivedHandler? LineReceived;
        private void initialize(string command, string arguments, List<string> commands)
        {
            initialCommands = commands;

            process = new System.Diagnostics.Process();

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += outputDataReceived;
            process.ErrorDataReceived += errorDataReceived;

            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
        }


        System.Diagnostics.Process? process = null;
        private List<string> initialCommands = new List<string>();

        public override void Start()
        {
            if (process == null) throw new Exception();
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            foreach (string command in initialCommands)
            {
                process.StandardInput.WriteLine(command);
            }
        }


        public override void Dispose()
        {
            if (process == null) return;

            KillProcessAndChildren(process.Id); // recursive process kill ( process.kill will not kill child process booted on cmd.exe );
            //process.WaitForExit();
            //process.Close();
            process.Close();
            process = null;
        }

        private static void KillProcessAndChildren(int pid)
        {
            //System.Management.ManagementObjectSearcher processSearcher = new System.Management.ManagementObjectSearcher
            //  ("Select * From Win32_Process Where ParentProcessID=" + pid);
            //System.Management.ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                System.Diagnostics.Process proc = System.Diagnostics.Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // already exited
            }

            //if (processCollection != null)
            //{
            //    foreach (System.Management.ManagementObject child in processCollection)
            //    {
            //        KillProcessAndChildrens(Convert.ToInt32(child["ProcessID"]));
            //    }
            //}
        }

        private bool logging = false;

        public override void StartLogging()
        {
            lock (logs)
            {
                logging = true;
                logs.Clear();
            }
        }

        public override void EndLogging()
        {
            lock (logs)
            {
                logging = false;
            }
        }

        public override void Execute(string command)
        {
            System.IO.StreamWriter sw = process.StandardInput;
            if (sw.BaseStream.CanWrite)
            {
                sw.WriteLine(command);
            }
        }

        List<string> logs = new List<string>();


        public override void ClearLogs()
        {
            lock (logs)
            {
                logs.Clear();
            }
        }

        public override List<string> GetLogs()
        {
            List<string> ret = new List<string>();
            lock (logs)
            {
                for (int i = 0; i < logs.Count; i++)
                {
                    ret.Add(logs[i]);
                }
            }
            return ret;
        }

        public override string GetLastLine()
        {
            lock (logs)
            {
                if (logs.Count == 0)
                {
                    return "";
                }
                else
                {
                    return logs.Last();
                }
            }
        }

        private void outputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (logs)
            {
                if (e.Data == null) return;
                if (e.Data == "") return;
                if (!logging) logs.Clear();
                logs.Add(e.Data);
                if (LineReceived != null) LineReceived(e.Data);
            }
        }


        private void errorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (logs)
            {
                if (e.Data == null) return;
                if (e.Data == "") return;
                if (!logging) logs.Clear();
                logs.Add(e.Data);
                if (LineReceived != null) LineReceived(e.Data);
            }
        }
    }
}
