using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.Tests
{
    public interface ITest
    {
        public Data.File? File { get; set; }
        public Task<string> RunSimulationAsync(CancellationToken cancellationToken);

        public Task<TestResult> GetSimulationResultAsync(CancellationToken cancellationToken);

        public Action<string, Avalonia.Media.Color?>? LogReceived { get; set; }

    }
}
