using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Shells
{
    public abstract class Shell : IDisposable
    {
        public Shell()
        {

        }

        public Shell(List<string> commands)
        {

        }

        public delegate void ReceivedHandler(string lineString);
        public virtual event ReceivedHandler? LineReceived;

        public virtual void Start()
        {
        }

        public virtual void Dispose()
        {
        }

        public virtual void StartLogging()
        {

        }

        public virtual void EndLogging()
        {

        }

        public virtual void Execute(string command)
        {
        }

        public virtual void ClearLogs()
        {

        }

        public virtual List<string> GetLogs()
        {
            return new List<string>();
        }

        public virtual string GetLastLine()
        {
            return "";
        }
    }
}
