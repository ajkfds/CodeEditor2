using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AjkAvaloniaLibs.Libs.Json;
using Global = CodeEditor2.Global;

namespace CodeEditor2.FileTypes
{
    public class FileAttributeList
    {

        public void ReadJson(JsonReader reader)
        {
            while (true)
            {
                string key = reader.GetNextKey();
                if (key == null) break;

                //if (items.ContainsKey(key))
                //{
                //    items[key].ReadJson(reader);
                //}
                //else
                {
                    reader.SkipValue();
                }
            }
        }
        public void WriteJson(JsonWriter writer)
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

            //using (var blockWriter = writer.GetObjectWriter("Projects"))
            //{
            //    foreach (var projectKvp in Global.Projects)
            //    {
            //        using (var projectWriter = blockWriter.GetObjectWriter(projectKvp.Key))
            //        {
            //            projectKvp.Value.SaveSetup(projectWriter);
            //        }
            //    }
            //}

        }
    }
}
