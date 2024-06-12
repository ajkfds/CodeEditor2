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

        public void SaveSetup(string path)
        {
            LastUpdate = DateTime.Now;

            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            options.Converters.Add(new ProjectPropertyJsonConverter());

            ProjectSetups.Clear();
            foreach (Project project in Global.Projects.Values)
            {
                ProjectSetups.Add(project.CreateSetup());
            }

            using (FileStream file = System.IO.File.Create(path))
            {
                System.Text.Json.JsonSerializer.Serialize(file, this, options);
            }

        }
        public async Task LoadSetup(string path)
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            };
            options.Converters.Add(new ProjectPropertyJsonConverter());

            ProjectSetups.Clear();
            foreach (Project project in Global.Projects.Values)
            {
                ProjectSetups.Add(project.CreateSetup());
            }

            using (FileStream file = System.IO.File.Open(path, FileMode.Open))
            {
                Setup? setup = System.Text.Json.JsonSerializer.Deserialize<Setup>(file, options);
                if (setup == null) return;

                if (setup.ApplicationName != ApplicationName) return;
                LastUpdate = setup.LastUpdate;
                foreach (var projectSetup in setup.ProjectSetups)
                {
                    if (projectSetup == null) continue;
                    if (Global.Projects.ContainsKey(projectSetup.Name)) continue;

                    Project project = Project.Create(projectSetup);
                    await CodeEditor2.Controller.AddProject(project);
                }
            }
        }

        public class ProjectPropertyJsonConverter : JsonConverter<ProjectProperty.Setup>
        {
            public override ProjectProperty.Setup Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                long index = reader.TokenStartIndex;
                JsonElement je = JsonElement.ParseValue(ref reader);
                JsonElement jid;

                je.TryGetProperty("ID", out jid);
                string? id = jid.GetString();
                if (id == null) return null;
                if (Global.ProjectPropertyDeserializers.ContainsKey(id))
                {
                    return Global.ProjectPropertyDeserializers[id](je, options);
                }

                return null;
            }

            public override void Write(
                Utf8JsonWriter writer,
                ProjectProperty.Setup value,
                JsonSerializerOptions options)
            {
                value.Write(writer, options);
            }
        }

//        public class ProjectPropertyJsonConverter2 : JsonConverter<Dictionary<string,ProjectProperty.Setup>>
//        {
//            public override Dictionary<string, ProjectProperty.Setup> Read(
//                ref Utf8JsonReader reader,
//                Type typeToConvert,
//                JsonSerializerOptions options)
//            {
//                long index = reader.TokenStartIndex;
//                JsonElement je = JsonElement.ParseValue(ref reader);
//                //JsonObject jo = JsonObject.Create(reader);

//                return (Dictionary<string, ProjectProperty.Setup>)JsonSerializer.Deserialize(ref reader, typeof(Dictionary<string, ProjectProperty.Setup>), options);
//            }

//            public override void Write(
//                Utf8JsonWriter writer,
//                Dictionary<string, ProjectProperty.Setup> value,
//                JsonSerializerOptions options)
//            {
////                value.Write(writer, options);
//            }
//        }


        // json serialize items
        public string ApplicationName { get; set; } = "CodeEditor2";
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public List<Project.Setup> ProjectSetups { set; get; } = new List<Project.Setup>();


    }
}
