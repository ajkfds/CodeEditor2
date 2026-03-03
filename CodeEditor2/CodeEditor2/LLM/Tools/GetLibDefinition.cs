using Avalonia.Controls.Documents;
using CodeEditor2.Data;
using CodeEditor2Plugin;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;


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

                return result;
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
