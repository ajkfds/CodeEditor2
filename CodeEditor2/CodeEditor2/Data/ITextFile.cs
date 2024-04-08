using CodeEditor2.CodeEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    public interface ITextFile
    {
        // item
        Data.Item Parent { get; set; }

        string ID { get; }
        string RelativePath { get; }

        string Name { get; }
        Project Project { get; }

        Item.ItemList Items { get; }

        Item GetItem(string relativePath);

        List<Item> FindItems(Func<Item, bool> match, Func<Item, bool> stop);

        bool ParseValid { get; }
        void Dispose();

        void Update();
        void ParseHierarchy(Action<ITextFile> action);

        NavigatePanel.NavigatePanelNode NavigatePanelNode { get; }
        CodeEditor.DocumentParser CreateDocumentParser(CodeEditor.DocumentParser.ParseModeEnum parseMode);

        // textFile
        CodeEditor.CodeDocument CodeDocument { get; }
        bool IsCodeDocumentCashed { get; }

        CodeEditor.ParsedDocument ParsedDocument { get; set; }
        void AcceptParsedDocument(CodeEditor.ParsedDocument newParsedDocument);

        void LoadFormFile();
        bool ReparseRequested { get; }
        // projectItem

        bool Dirty { get; }

        void Save();
        DateTime? LoadedFileLastWriteTime { get; }

        //        void AfterKeyPressed(System.Windows.Forms.KeyPressEventArgs e);
        //        void AfterKeyDown(System.Windows.Forms.KeyEventArgs e);
        //        void BeforeKeyPressed(System.Windows.Forms.KeyPressEventArgs e);
        //        void BeforeKeyDown(System.Windows.Forms.KeyEventArgs e);

//        PopupItem GetPopupItem(ulong Version, int index);
        List<CodeEditor2.CodeEditor.AutocompleteItem> GetAutoCompleteItems(int index, out string cantidateText);
        List<CodeEditor2.CodeEditor.ToolItem> GetToolItems(int index);


         CodeDrawStyle DrawStyle { get; }
    }
}
