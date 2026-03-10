using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
        protected Item() { }

        /*
        
        Item        <-- File    <-- TextFile
                    <-- Folder  <-- Project
         
        
        (ITextFile) <-- TextFile
        */



        /// <summary>
        /// Debug feature: Retrieve the Object ID to distinguish individual objects.
        /// </summary>
        public long ObjectID
        {
            get
            {
                bool firstTime;
                return Global.ObjectIDGenerator.GetId(this, out firstTime);
            }
        }

        // Maintain references to parent Items in the tree structure.
        // Items hold references from parent to child, while references to parents are held as weak references.
        // This unidirectional reference ensures that when a parent Item is discarded, unused child Items can be collected by the Garbage Collector.

        private WeakReference<Item>? parent;
        public Item? Parent
        {
            get
            {
                Item? ret;
                if (parent == null) return null;
                if (!parent.TryGetTarget(out ret)) return null;
                return ret;
            }
            set
            {
                if (value == null) return;
                parent = new WeakReference<Item>(value);
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
            // 繝励Λ繧ｰ繧､繝ｳ遲峨°繧芽ｦ九▽縺九▲縺滓ｴｾ逕溘け繝ｩ繧ｹ繧偵％縺薙↓逋ｻ骭ｲ縺励※縺・￥
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
                    // 繝・ヵ繧ｩ繝ｫ繝亥､縺ｮ繝励Ο繝代ユ繧｣繧偵☆縺ｹ縺ｦ髯､螟悶☆繧・//                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    // 隱ｭ縺ｿ蜿悶ｊ蟆ら畑繝励Ο繝代ユ繧｣繧堤┌隕悶☆繧・                    IgnoreReadOnlyProperties = true,
                    // 繧ｷ繝ｪ繧｢繝ｩ繧､繧ｺ譎ゅ↓繧ｪ繝悶ず繧ｧ繧ｯ繝医↓ $id・井ｸ諢上・ID・峨ｒ謖ｯ繧翫・蝗樒岼莉･髯阪・逋ｻ蝣ｴ譎ゅ・ $ref・亥盾辣ｧID・峨→縺励※險倬鹸縺励∪縺・                    ReferenceHandler = ReferenceHandler.Preserve,
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
                using(StreamReader sr = new StreamReader(path))
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
        }

        public virtual bool Ignore
        {
            get; set;
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

        public virtual void CheckStatus()
        {

        }

        private bool isDeleted = false;
        public virtual bool IsDeleted
        {
            get
            {
                return isDeleted;
            }
            set 
            {
                isDeleted = value;
            }
        }

        public virtual FileSystemInfo? FileSystemInfo { get; internal set; }
        public virtual void Remove()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Remove();
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
                    if(!itemDict.ContainsKey(key)) return false;
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

            // Enumerator縺ｮ螳溯｣・ｼ医せ繝翫ャ繝励す繝ｧ繝・ヨ繧定ｿ斐☆・・            public IEnumerator<Item> GetEnumerator()
            {
                List<Item> snapshot;
                lock (_lock)
                {
                    // 迴ｾ蝨ｨ縺ｮ繝ｪ繧ｹ繝医・迥ｶ諷九ｒ譁ｰ縺励＞繝ｪ繧ｹ繝医↓繧ｳ繝斐・・医せ繝翫ャ繝励す繝ｧ繝・ヨ・・                    snapshot = itemList.ToList();
                }

                // 繧ｳ繝斐・縺励◆繝ｪ繧ｹ繝医・Enumerator繧定ｿ斐☆
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
                if(items.TryGetValue(folderName, out Item? item))
                {
                    if(item == null) throw new Exception("item is null");
                    return item.GetItem(relativePath.Substring(folderName.Length + 1));
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if(items.TryGetValue(relativePath, out Item? item))
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
            findItems(result, match, stop,action);
            return result;
        }

        protected void findItems(List<Item> result, Func<Item, bool> match, Func<Item, bool> stop, Action<Item>? action)
        {
            foreach (Item item in items)
            {
                if (match(item))
                {
                    result.Add(item);
                    if(action != null) action(item);
                }
                if (!stop(item)) item.findItems(result, match, stop,action);
            }
        }

        public virtual Task SyncStatus()
        {
            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
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


        protected NavigatePanel.NavigatePanelNode? node;
        public virtual NavigatePanel.NavigatePanelNode NavigatePanelNode
        {
            get
            {
                if (node == null) node = CreateNode();
                return node;
            }
            protected set
            {
                node = value;
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
