using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AjkAvaloniaLibs.Libs.Json;

namespace CodeEditor2.Data
{
    public class ProjectProperty
    {
        public virtual void SaveSetup(JsonWriter writer)
        {

        }

        public virtual void LoadSetup(JsonReader jsonReader)
        {
            using (var reader = jsonReader.GetNextObjectReader())
            {
                while (true)
                {
                    string key = reader.GetNextKey();
                    if (key == null) break;

                    reader.SkipValue();
                }
            }
        }
    }
}
