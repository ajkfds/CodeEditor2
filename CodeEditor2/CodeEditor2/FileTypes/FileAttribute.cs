using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AjkAvaloniaLibs.Libs.Json;

namespace CodeEditor2.FileTypes
{
    public class FileAttribute
    {
        public static bool HasThisAttribule()
        {
            return false;
        }

        public string Filter;
        public void ReadJson(JsonReader reader)
        {
            while (true)
            {
                string key = reader.GetNextKey();
                if (key == null) break;

                switch (key)
                {
                    case "Filter":
                        Filter = reader.GetNextStringValue();
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }
        }
        public void WriteJson(JsonWriter writer)
        {
            using (var blockWriter = writer.GetObjectWriter("FileAttribute"))
            {
                blockWriter.writeKeyValue("Filter", Filter);
            }
        }
    }
}
