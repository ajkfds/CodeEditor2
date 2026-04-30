using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    /// <summary>
    /// Item is the basic unit of all Data. 
    /// All items represent specific folders or files, and project/Directory/File objects are all represented as items that inherit from Item.
    /// This Item holds the associated data, and that data is reflected in the UI.
    /// </summary>
    //    [JsonDerivedType(typeof(Item), typeDiscriminator: "Item")]
    //    [JsonDerivedType(typeof(Folder), typeDiscriminator: "Folder")]
    //    [JsonDerivedType(typeof(Project), typeDiscriminator: "Project")]
    //    [JsonDerivedType(typeof(TextFile), typeDiscriminator: "TextFile")]
    public class Item : IDisposable
    {
        /// <summary>
        /// Lock for thread-safe access to Item properties
        /// </summary>
        protected readonly ReaderWriterLockSlim itemLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        protected Item() { }

        /*

        Item        <-- File    <-- TextFile
                    <-- Folder  <-- Project


        (ITextFile) <-- TextFile
        */




        // Maintain references to parent Items in the tree structure.
        // Items hold references from parent to child, while references to parents are held as weak references.
        // This unidirectional reference ensures that when a parent Item is discarded, unused child Items can be collected by the Garbage Collector.

        private WeakReference<Item>? parent;
        public Item? Parent
        {
            get
            {
                itemLock.EnterReadLock();
                try
                {
                    Item? ret;
                    if (parent == null) return null;
                    if (!parent.TryGetTarget(out ret)) return null;
                    return ret;
                }
                finally
                {
                    itemLock.ExitReadLock();
                }
            }
            set
            {
                if (value == null) return;
                itemLock.EnterWriteLock();
                try
                {
                    parent = new WeakReference<Item>(value);
                }
                finally
                {
                    itemLock.ExitWriteLock();
                }
            }
        }


        static Item()
        {
            Data.Item.PolymorphicResolver.DerivedTypes.Add(new JsonDerivedType(typeof(Data.Project)));
            Data.Item.PolymorphicResolver.DerivedTypes.Add(new JsonDerivedType(typeof(Data.File)));
            Data.Item.PolymorphicResolver.DerivedTypes.Add(new JsonDerivedType(typeof(Data.Folder)));
        }
        public static class PolymorphicResolver
        {
            // プラグイン等から見つかった派生クラスをここに登録していく
            public static List<JsonDerivedType> DerivedTypes { get; } = new();

            public static void MyTypeResolver(JsonTypeInfo jsonTypeInfo)
            {
                if (jsonTypeInfo.Type == typeof(Item))
                {
                    jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                    {
                        TypeDiscriminatorPropertyName = "$type",
                        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
                    };

                    foreach (var type in DerivedTypes)
                    {
                        jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(type);
                    }
                }

                if (jsonTypeInfo.Type == typeof(ParsedDocument))
                {
                    jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                    {
                        TypeDiscriminatorPropertyName = "$type",
                        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
                    };

                    foreach (var type in ParsedDocument.DerivedTypes)
                    {
                        jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(type);
                    }
                }

                if (jsonTypeInfo.Type == typeof(ProjectProperty))
                {
                    jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                    {
                        TypeDiscriminatorPropertyName = "$type",
                        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
                    };

                    foreach (var type in ProjectProperty.DerivedTypes)
                    {
                        jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(type);
                    }
                }

            }
        }

        public static JsonSerializerOptions SerializerOptions
        {
            get
            {
                var options = new JsonSerializerOptions
                {
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver
                    {
                        Modifiers = { PolymorphicResolver.MyTypeResolver }
                    },
                    WriteIndented = true,
                    // デフォルト値のプロパティをすべて除外する
                    //                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    // 読み取り専用プロパティを無視する
                    IgnoreReadOnlyProperties = true,
                    // シリアライズ時にオブジェクトに $id（一意のID）を振り、2回目以降の登場時は $ref（参照ID）として記録します
                    ReferenceHandler = ReferenceHandler.Preserve,
                };

                return options;
            }
        }

        public void Serialize(string path)
        {
            string json = JsonSerializer.Serialize(this, SerializerOptions);
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(json);
            }
        }

        public static Item? Deserialize(string path)
        {
            try
            {
                string json;
                using (StreamReader sr = new StreamReader(path))
                {
                    json = sr.ReadToEnd();
                }
                Item? item = JsonSerializer.Deserialize<Item>(json, SerializerOptions);
                return item;
            }
            catch
            {
                CodeEditor2.Controller.AppendLog("failed to deserialize " + path);
                return null;
            }
        }
        public virtual string ID
        {
            get
            {
                return RelativePath;
            }
            set
            {

            }
        }

        private bool ignore = false;
        public virtual bool Ignore
        {
            get
            {
                itemLock.EnterReadLock();
                try
                {
                    return ignore;
                }
                finally
                {
                    itemLock.ExitReadLock();
                }
            }
            set
            {
                itemLock.EnterWriteLock();
                try
                {
                    ignore = value;
                }
                finally
                {
                    itemLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// file relative path
        /// </summary>
        public required virtual string RelativePath { get; init; }

        public required virtual string Name { get; init; }

        /// <summary>
        /// refrence to root project item
        /// </summary>
        public required virtual Project Project { get; init; }

        protected ItemList items = new ItemList();
        /// <summary>
        /// holding child Items
        /// </summary>
        [JsonInclude]
        public virtual ItemList Items
        {
            get { return items; }
            private set { items = value; }
        }

        public virtual void PostStatusCheck()
        {

        }

        private bool isDeleted = false;
        public virtual bool IsDeleted
        {
            get
            {
                itemLock.EnterReadLock();
                try
                {
                    return isDeleted;
                }
                finally
                {
                    itemLock.ExitReadLock();
                }
            }
            set
            {
                itemLock.EnterWriteLock();
                try
                {
                    isDeleted = value;
                }
                finally
                {
                    itemLock.ExitWriteLock();
                }
            }
        }

        private FileSystemInfo? fileSystemInfo = null;
        public virtual FileSystemInfo? FileSystemInfo
        {
            get
            {
                itemLock.EnterReadLock();
                try
                {
                    return fileSystemInfo;
                }
                finally
                {
                    itemLock.ExitReadLock();
                }
            }
            internal set
            {
                itemLock.EnterWriteLock();
                try
                {
                    fileSystemInfo = value;
                }
                finally
                {
                    itemLock.ExitWriteLock();
                }
            }
        }

        public virtual void Remove()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => { Remove(); });
                return;
            }
            if (Parent != null)
            {
                Parent.Items.TryRemove(Name);
                Parent.NavigatePanelNode.UpdateVisual();
            }
        }


        /// <summary>
        /// This is a collection class for holding child Items. It has the functionality of both a dictionary and a list.
        /// </summary>
        public class ItemList : IEnumerable<Item>
        {
            private readonly List<Item> itemList = new List<Item>();
            private readonly Dictionary<string, Item> itemDict = new Dictionary<string, Item>();
            private readonly object _lock = new object();
            public void AddOrUpdate(string key, Item item)
            {
                lock (_lock)
                {
                    if (itemDict.ContainsKey(key))
                    {
                        itemList[itemList.IndexOf(itemDict[key])] = item;
                        itemDict[key] = item;
                        return;
                    }
                    itemList.Add(item);
                    itemDict.Add(key, item);
                }
            }

            //public bool TryInsert(int index, string key, Item item)
            //{
            //    lock (_lock)
            //    {
            //        if (itemDict.ContainsKey(key)) return false;
            //        itemList.Insert(index, item);
            //        itemDict.Add(key, item);
            //        return true;
            //    }
            //}

            public int IndexOf(Item item)
            {
                lock (_lock)
                {
                    return itemList.IndexOf(item);
                }
            }

            public bool TryRemove(string key)
            {
                lock (_lock)
                {
                    if (!itemDict.ContainsKey(key)) return false;
                    itemList.Remove(itemDict[key]);
                    itemDict.Remove(key);
                    return true;
                }
            }
            public bool TryGetValue(string key, out Item? value)
            {
                lock (_lock)
                {
                    return itemDict.TryGetValue(key, out value);
                }
            }

            public void Clear()
            {
                lock (_lock)
                {
                    itemList.Clear();
                    itemDict.Clear();
                }
            }

            public void Sort(Comparison<Item> comparison)
            {
                lock (_lock)
                {
                    itemList.Sort(comparison);
                }
            }

            // Enumeratorの実装（スナップショットを返す）
            public IEnumerator<Item> GetEnumerator()
            {
                List<Item> snapshot;
                lock (_lock)
                {
                    // 現在のリストの状態を新しいリストにコピー（スナップショット）
                    snapshot = itemList.ToList();
                }

                // コピーしたリストのEnumeratorを返す
                foreach (var item in snapshot)
                {
                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }


        public virtual Item? GetItem(string relativePath)
        {
            // hierarchy search to get a item
            if (Name == relativePath) return this;

            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                string folderName = relativePath.Substring(0, relativePath.IndexOf(System.IO.Path.DirectorySeparatorChar));
                if (items.TryGetValue(folderName, out Item? item))
                {
                    if (item == null) throw new Exception("item is null");
                    return item.GetItem(relativePath.Substring(folderName.Length + 1));
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (items.TryGetValue(relativePath, out Item? item))
                {
                    return item;
                }
                else
                {
                    return null;
                }
            }
        }

        public virtual List<Item> FindItems(Func<Item, bool> match, Func<Item, bool> stop)
        {
            return FindItems(match, stop, null);
        }
        public virtual List<Item> FindItems(Func<Item, bool> match, Func<Item, bool> stop, Action<Item>? action)
        {
            List<Item> result = new List<Item>();
            findItems(result, match, stop, action);
            return result;
        }

        protected void findItems(List<Item> result, Func<Item, bool> match, Func<Item, bool> stop, Action<Item>? action)
        {
            foreach (Item item in items)
            {
                if (match(item))
                {
                    result.Add(item);
                    if (action != null) action(item);
                }
                if (!stop(item)) item.findItems(result, match, stop, action);
            }
        }

        public virtual Task SyncStatus()
        {
            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            //try
            //{
            //    itemLock?.Dispose();
            //}
            //catch (System.Threading.SynchronizationLockException)
            //{
            //    // Lock was held by another thread or current thread during disposal
            //    // This can happen during application shutdown or race conditions
            //}
        }

        /// <summary>
        /// Update Data Items
        /// </summary>
        /// <returns></returns>
        public virtual System.Threading.Tasks.Task UpdateAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void PostUIUpdate()
        {

        }

        public static Action<ContextMenu>? CustomizeItemEditorContextMenu;

        public void CustomizeEditorContextMenu(ContextMenu contextMenu)
        {
            CustomizeItemEditorContextMenu?.Invoke(contextMenu);
        }


        private NavigatePanel.NavigatePanelNode? node = null;
        public virtual NavigatePanel.NavigatePanelNode NavigatePanelNode
        {
            get
            {
                if (node == null)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        // すでに他で生成されていないか再チェック（ダブルチェック）
                        if (node == null)
                        {
                            node = CreateNode();
                        }
                    });
                }
                if (node == null) throw new Exception();
                return node;
            }
            protected set
            {
                itemLock.EnterWriteLock();
                try
                {
                    node = value;
                }
                finally
                {
                    itemLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// create navigate panel node for this item
        /// overrride this method to extend custom navigate node
        /// </summary>
        /// <returns></returns>
        protected virtual NavigatePanel.NavigatePanelNode CreateNode()
        {
            // should set nodeRef
            System.Diagnostics.Debugger.Break();
            return NavigatePanelNode;
        }

        /// <summary>
        /// Create document parser for this item
        /// override this method to create custom document parser
        /// </summary>
        /// <param name="parseMode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual DocumentParser? CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return null;
        }


        //public virtual NavigatePanel.NavigatePanelNode? CreateLinkNode()
        //{
        //    NavigatePanel.NavigatePanelNode node;
        //    node = CreateNode();
        //    return node;
        //}



    }
}
