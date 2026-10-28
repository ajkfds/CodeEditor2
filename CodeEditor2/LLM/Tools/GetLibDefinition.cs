using Microsoft.Extensions.AI;
using System;
using System.ComponentModel;
using System.IO;


namespace CodeEditor2.LLM.Tools
{
    public class GetLibDefinition : LLMTool
    {
        public GetLibDefinition(Data.Project project) : base(project) { }
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "get_lib_definition"); }

        public override string XmlExample { get; } = """
            ```xml
            <get_lib_definition>
            <path>library type name here</path>
            </get_lib_definition>         
            ```
            """;

        [Description("""
            Request to get a definition of the library type name. 
            Note: there is a size limit on the returned definition. If it exceeds the limit,
            a warning is prepended and only the beginning (first part) of the definition is returned;
            the tail is truncated.
            """)]
        public string Run(
            [Description("The library path name")] string library_type_name)
        {
            try
            {
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                string? result = DocExtractor.GetDocument(library_type_name);

                if (result == null)
                {
                    return $"Error: Library definition not found for '{library_type_name}'.";
                }

                // 出力サイズ上限 (LLMTool.MaxOutputBytes) を適用。超過時はアラート + 前側のみ返す
                return LimitOutput(result, "get_lib_definition");
            }
            catch (UnauthorizedAccessException)
            {
                return "Error: Access to the path is denied.";
            }
            catch (IOException ex)
            {
                return $"Error: An I/O error occurred while reading the file: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: An unexpected error occurred: {ex.Message}";
            }
        }
    }



}
