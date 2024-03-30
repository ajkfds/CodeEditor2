using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.Data;
using AjkAvaloniaLibs.Libs.Json;

namespace CodeEditor2.Setups
{
    public class Setup
    {

        public void SaveSetup(string path)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
            {
                using (JsonWriter writer = new JsonWriter(sw))
                {
                    writeJson(writer);
                }
            }
        }

        public void LoadSetup(string path)
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader(path))
            {
                using (JsonReader reader = new JsonReader(sr))
                {
                    readJson(reader);
                }
            }
        }

        private void readJson(JsonReader reader)
        {
            while (true)
            {
                string key = reader.GetNextKey();
                if (key == null) break;

                switch (key)
                {
                    case "CodeEditor2":
                        readCodeEditorSetup(reader);
                        break;
                    case "PluginSetups":
                        readPluginSetup(reader);
                        break;
                    case "Projects":
                        readProjects(reader);
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }
        }

        private void readCodeEditorSetup(JsonReader jsonReader)
        {
            using (var reader = jsonReader.GetNextObjectReader())
            {
                while (true)
                {
                    string key = reader.GetNextKey();
                    if (key == null) break;

                    switch (key)
                    {
                        case "ApplicationName":
                            string applicationName = reader.GetNextStringValue();
                            if (applicationName != "CodeEditor2") throw new Exception("illegal format");
                            break;
                        case "LastUpdate":
                            string lastUpdate = reader.GetNextStringValue();
                            break;
                        default:
                            reader.SkipValue();
                            break;
                    }
                }
            }
        }

        private void readPluginSetup(JsonReader jsonReader)
        {
            using (var reader = jsonReader.GetNextObjectReader())
            {
                while (true)
                {
                    string key = reader.GetNextKey();
                    if (key == null) break;

                    //if (Global.PluginSetups.ContainsKey(key))
                    //{
                    //    using (var block = reader.GetNextObjectReader())
                    //    {
                    //        Global.PluginSetups[key].ReadJson(block);
                    //    }
                    //}
                    //else
                    {
                        reader.SkipValue();
                    }
                }
            }
        }

        private async Task readProjects(JsonReader jsonReader)
        {
            List<Project> projects = new List<Project>();

            using (var reader = jsonReader.GetNextObjectReader())
            {
                while (true)
                {
                    string key = reader.GetNextKey();
                    if (key == null) break;

                    if (Global.Projects.ContainsKey(key))
                    {
                        Global.Projects[key].LoadSetup(reader);
                    }
                    else
                    {
                        Project project = Project.Create(reader);
                        projects.Add(project);
                    }
                }
            }
            foreach(Project project in projects)
            {
                Controller.AddProject(project);
            }
        }


        private void writeJson(JsonWriter writer)
        {
            using (var blockWriter = writer.GetObjectWriter("CodeEditor2"))
            {
                blockWriter.writeKeyValue("ApplicationName", "CodeEditor2");
                blockWriter.writeKeyValue("LastUpdate", DateTime.Now.ToString());
            }

            //using (var blockWriter = writer.GetObjectWriter("PluginSetups"))
            //{
            //    foreach (var pluginKvp in Global.PluginSetups)
            //    {
            //        using (var pluginWriter = blockWriter.GetObjectWriter(pluginKvp.Key))
            //        {
            //            pluginKvp.Value.SaveSetup(pluginWriter);
            //        }
            //    }
            //}

            using (var blockWriter = writer.GetObjectWriter("Projects"))
            {
                foreach (var projectKvp in Global.Projects)
                {
                    using (var projectWriter = blockWriter.GetObjectWriter(projectKvp.Key))
                    {
                        projectKvp.Value.SaveSetup(projectWriter);
                    }
                }
            }

        }
    }
}
