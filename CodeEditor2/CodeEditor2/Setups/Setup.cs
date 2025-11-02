using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.Data;
using AjkAvaloniaLibs.Libs.Json;
using System.Text.Json;
using System.Data;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace CodeEditor2.Setups
{
    public class Setup
    {
        public　static string ApplicationName { get; set; } = "CodeEditor2";
        public static string Path { get; set; } = "History.json";
        public DateTime LastUpdate { get; set; } = DateTime.Now;

        public List<History> Historys { set; get; } = new List<History>();

        public class History
        {
            public History() { }
            public string Name { set; get; } = "";
            public DateTime LastAccessed { set; get; }
            public string AbsolutePath { get; set; } = "";

            public int? PinnedOrder = null;
        }

        public void SaveSetup()
        {
            LastUpdate = DateTime.Now;

            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };

            using (FileStream file = System.IO.File.Create(Path))
            {
                System.Text.Json.JsonSerializer.Serialize(file, this, options);
            }

        }
        public void LoadSetup()
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            };

            if (!System.IO.File.Exists(Path)) return;
            using (FileStream file = System.IO.File.Open(Path, FileMode.Open))
            {
                Setup? setup = System.Text.Json.JsonSerializer.Deserialize<Setup>(file, options);
                if( setup != null) Global.Setup = setup;
            }
        }

    }
}
