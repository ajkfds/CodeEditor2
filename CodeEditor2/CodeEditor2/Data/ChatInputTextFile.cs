using Avalonia.Media;
using CodeEditor2.CodeEditor.CodeComplete;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CodeEditor2.Data
{
    /// <summary>
    /// A Project used only to satisfy the required Project reference on
    /// ChatInputTextFile. It is not registered in Global.Projects, never
    /// saved, and never holds any real file items. The protected
    /// Project(string, string, string) constructor is invoked here from a
    /// derived class within the same assembly, which is the only place it is
    /// accessible.
    /// </summary>
    public class ChatInputProject : Project
    {
        [SetsRequiredMembers]
        public ChatInputProject()
            : base(name: "<chat_input>",
                   rootPath: "",
                   relativePath: "<chat_input>")
        { }
    }

    /// <summary>
    /// TextFile used by the LLM Chat input.
    /// This file is not associated with any real Project on disk, and its
    /// GetAutoCompleteItems returns candidates based on identifiers already
    /// present in the chat input text itself, without depending on the
    /// MainWindow's codeView.
    /// </summary>
    public class ChatInputTextFile : TextFile
    {
        public ChatInputTextFile() : base()
        {
            // The required Item members (Project, RelativePath, Name) are
            // set as part of the object initializer expression in
            // CreateInstance(), so they are assigned before the base
            // constructor (and thus Item's constructor) runs.
            CreateCodeDocument();
        }

        /// <summary>
        /// Factory that returns a fully-initialized ChatInputTextFile via an
        /// object initializer. Using an object initializer ensures that the
        /// required Item.Project / Item.RelativePath / Item.Name members
        /// are assigned before the TextFile base constructor (and hence
        /// Item's constructor) completes, which is required by C# 11's
        /// 'required' + 'init' semantics.
        /// </summary>
        public static ChatInputTextFile CreateInstance()
        {
            Project project = ResolveProject();
            ChatInputTextFile instance = new ChatInputTextFile
            {
                Project = project,
                RelativePath = "<chat_input>",
                Name = "<chat_input>",
            };
            return instance;
        }

        private static Project ResolveProject()
        {
            if (Global.Projects != null && Global.Projects.Count > 0)
            {
                foreach (Project p in Global.Projects.Values)
                {
                    return p;
                }
            }
            // As a last resort, build an in-memory Project that is never
            // registered or saved. The Project's protected constructor is
            // only accessible from a derived class within the same assembly.
            return new ChatInputProject();
        }

        /// <summary>
        /// Mirror the text of the InputItem's TextEditor into this file's
        /// CodeDocument so auto-complete candidates reflect the current
        /// chat input contents.
        /// </summary>
        public void MirrorText(string text)
        {
            if (CodeDocument == null) return;
            CodeDocument.TextDocument.Text = text ?? "";
        }

        /// <summary>
        /// Returns auto-complete candidates for the chat input.
        /// Scans the current chat input text and proposes identifiers already
        /// present in the buffer as candidates.
        /// </summary>
        public override List<AutocompleteItem>? GetAutoCompleteItems(int index, out string? candidateWord)
        {
            candidateWord = "";
            if (CodeDocument == null) return null;

            // Extract current word (identifier-like) at index
            string text = CodeDocument.CreateString();
            if (string.IsNullOrEmpty(text)) return null;
            if (index <= 0 || index > text.Length) return null;

            int wordStart = index - 1;
            while (wordStart >= 0 && IsWordChar(text[wordStart]))
            {
                wordStart--;
            }
            wordStart++;

            if (wordStart >= index) return null;
            candidateWord = text.Substring(wordStart, index - wordStart);

            // Collect unique identifiers from the text
            var matches = Regex.Matches(text, @"[A-Za-z_$][A-Za-z0-9_$]*");
            var seen = new HashSet<string>();
            var items = new List<AutocompleteItem>();
            foreach (Match m in matches)
            {
                string word = m.Value;
                if (word.Length < 2) continue;
                if (seen.Contains(word)) continue;
                if (!string.IsNullOrEmpty(candidateWord) && !word.StartsWith(candidateWord)) continue;
                seen.Add(word);
                items.Add(new AutocompleteItem(
                    word,
                    0,
                    Color.FromRgb(212, 212, 212)
                    ));
            }

            return items;
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '$';
        }
    }
}
