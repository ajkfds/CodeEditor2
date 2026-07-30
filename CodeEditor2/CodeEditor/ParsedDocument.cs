using CodeEditor2.CodeEditor.Parser;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CodeEditor2.CodeEditor
{
    /// <summary>
    // ParsedDocument is the parse result of a text document.
    // The Parser object parses the TextFile and returns a ParsedDocument object. It keeps the parse results.
    /// </summary>
    public class ParsedDocument : IDisposable
    {
        public ParsedDocument(Data.TextFile textFile, string key, ulong version, DocumentParser.ParseModeEnum parseMode)
        {
            this.Version = version;
            this.ParseMode = parseMode;
            this.Key = key;
            textFileRef = new WeakReference<Data.TextFile>(textFile);
        }


        // プラグイン等から見つかった派生クラスをここに登録していく
        public static List<JsonDerivedType> DerivedTypes { get; } = new();

        public string Key { get; set; }



        [JsonIgnore]
        public List<CodeDocument> LockedDocument = new List<CodeDocument>();

        public readonly DocumentParser.ParseModeEnum ParseMode;

        private System.WeakReference<Data.TextFile> textFileRef;
        [JsonIgnore]
        public Data.Item? Item
        {
            get
            {
                Data.TextFile? ret;
                if (!textFileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public Data.TextFile? TextFile
        {
            get
            {
                Data.TextFile? ret;
                if (!textFileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        public Data.Project? Project
        {
            get
            {
                if (Item == null) return null;
                return Item.Project;
            }
        }
        public ulong Version { get; set; }

        public virtual void Dispose()
        {
        }

        public List<Message> Messages = new List<Message>();

        public class Message
        {
            public int Index { get; protected set; }
            public int Length { get; protected set; }
            public string Text { get; protected set; } = "";

            public Data.Project? Project { get; protected set; } = null;
            public virtual MessageView.MessageNode? CreateMessageNode()
            {
                return null;
            }
        }

    }
}
