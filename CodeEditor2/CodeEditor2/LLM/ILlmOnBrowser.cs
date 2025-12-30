using Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.LLM
{
    public interface ILlmOnBrowser
    {
        public void Launch()
        {

        }
        public void Reset()
        {

        }

        public Task<string?> Ask(string prompt)
        {
            return Task.FromResult<string?>(null);
        }

    }
}
