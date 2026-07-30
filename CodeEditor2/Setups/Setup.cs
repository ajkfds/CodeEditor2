using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace CodeEditor2.Setups
{
    public class Setup
    {
        public static string ApplicationName { get; set; } = "CodeEditor2";

        public static Action<Window> InitializeWindow =
            (Window) =>
            {
                using (var stream = AssetLoader.Open(new Uri("avares://CodeEditor2/Assets/CodeEditor2.ico")))
                {
                    Window.Icon = new WindowIcon(stream);
                }
            };

        public static Func<IImage>? GetIconImage = null;
        public static string Path { get; set; } = "History.json";
        public DateTime LastUpdate { get; set; } = DateTime.Now;

        public string PasswordHash { get; set; } = "";
        public string PasswordSalt { get; set; } = "";
        public string DerivedSalt { get; set; } = "";
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
                if (setup != null) Global.Setup = setup;
            }
            Global.IsBooting = false;
        }

    }
}
