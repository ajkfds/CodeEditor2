using CodeEditor2.Tools;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace CodeEditor2.Data
{
    /// <summary>
    /// Project property object attached to the project.
    /// Used to maintain setup for each project. 
    /// By inheriting this project property for each function and implementing the setup, 
    /// the project can be serialized and the settings saved to the project file when saving the project.
    /// </summary>
    public class ProjectProperty
    {
        public ProjectProperty(Project project)
        {

        }
        public ProjectProperty(Project project, Setup setup)
        {

        }

        // プラグイン等から見つかった派生クラスをここに登録していく
        public static List<JsonDerivedType> DerivedTypes { get; } = new();

        // Item Property Tab 追加用
        public virtual void InitializePropertyForm(ItemPropertyForm form, CodeEditor2.NavigatePanel.NavigatePanelNode node, Project project)
        {
        }

        public virtual Setup CreateSetup()
        {
            return new Setup(this);
        }

        public virtual Setup? CreateSetup(JsonElement jsonElement, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(jsonElement, typeof(Setup), options) as ProjectProperty.Setup;
        }

        public class Setup
        {
            public Setup() { }
            public Setup(ProjectProperty projectProperty)
            {

            }
            public virtual string ID { get; set; } = "default";

            public virtual void Write(
                Utf8JsonWriter writer,
                JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, this, typeof(Setup), options);
            }
        }


    }
}
