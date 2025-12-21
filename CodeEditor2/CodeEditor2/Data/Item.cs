using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor.Parser;

namespace CodeEditor2.Data
{
    /// <summary>
    /// Item is the basic unit of all Data. 
    /// All items represent specific folders or files, and project/Directory/File objects are all represented as items that inherit from Item.
    /// This Item holds the associated data, and that data is reflected in the UI.
    /// </summary>
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
        public virtual ItemList Items
        {
            get { return items; }
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
                if (isDeleted)
                {
                    if(Parent != null && Parent.Items.ContainsKey(Name))
                    {
                        Parent.Items.Remove(Name);
                    }
                    Dispose();
                }
                else
                {
                    if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                }
            }
        }

        /// <summary>
        /// This is a collection class for holding child Items. It has the functionality of both a dictionary and a list.
        /// </summary>
        public class ItemList
        {
            private List<Item> itemList = new List<Item>();
            private Dictionary<string, Item> itemDict = new Dictionary<string, Item>();

            public void Add(string key, Item item)
            {
                if (itemDict.ContainsKey(key)) return;
                itemList.Add(item);
                itemDict.Add(key, item);
            }

            public void Insert(int index, string key, Item item)
            {
                if (itemDict.ContainsKey(key)) return;
                itemList.Insert(index, item);
                itemDict.Add(key, item);
            }

            public int IndexOf(Item item)
            {
                return itemList.IndexOf(item);
            }

            public Item this[string key]
            {
                get
                {
                    return itemDict[key];
                }
            }

            public Item this[int index]
            {
                get
                {
                    return itemList[index];
                }
            }

            public void Remove(string key)
            {
                itemList.Remove(itemDict[key]);
                itemDict.Remove(key);
            }
            public bool ContainsKey(string key)
            {
                return itemDict.ContainsKey(key);
            }

            public bool ContainsValue(Item item)
            {
                return itemDict.ContainsValue(item);
            }

            public void Clear()
            {
                itemList.Clear();
                itemDict.Clear();
            }

            public Dictionary<string, Item>.KeyCollection Keys
            {
                get { return itemDict.Keys; }
            }

            public List<Item> Values
            {
                get
                {
                    return itemList;
                }
            }

        }


        public virtual Item? GetItem(string relativePath)
        {
            // hierarchy search to get a item
            if (Name == relativePath) return this;

            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                string folderName = relativePath.Substring(0, relativePath.IndexOf(System.IO.Path.DirectorySeparatorChar));
                if (items.ContainsKey(folderName))
                {
                    return items[folderName].GetItem(relativePath.Substring(folderName.Length + 1));
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (items.ContainsKey(relativePath))
                {
                    return items[relativePath];
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
            lock (Items)
            {
                foreach (Item item in items.Values)
                {
                    if (match(item))
                    {
                        result.Add(item);
                        if(action != null) action(item);
                    }
                    if (!stop(item)) item.findItems(result, match, stop,action);
                }
            }
        }


        public virtual void Dispose()
        {
        }

        public virtual System.Threading.Tasks.Task UpdateAsync()
        {
            return Task.CompletedTask;
        }

        public static Action<ContextMenu>? CustomizeItemEditorContextMenu;

        public void CustomizeEditorContextMenu(ContextMenu contextMenu)
        {
            CustomizeItemEditorContextMenu?.Invoke(contextMenu);
        }


        protected WeakReference<NavigatePanel.NavigatePanelNode>? nodeRef;
        public virtual NavigatePanel.NavigatePanelNode NavigatePanelNode
        {
            get
            {
                NavigatePanel.NavigatePanelNode? node;
                if (nodeRef == null)
                {
                    node = CreateNode();
                    if (node == null) throw new Exception();
                    nodeRef = new WeakReference<NavigatePanel.NavigatePanelNode>(node);
                    return node;
                }

                if (nodeRef.TryGetTarget(out node)) return node;

                node = CreateNode();
                if (node == null) throw new Exception();
                nodeRef = new WeakReference<NavigatePanel.NavigatePanelNode>(node);
                return node;
            }
            protected set
            {
                nodeRef = new WeakReference<NavigatePanel.NavigatePanelNode>(value);
            }
        }
        protected virtual NavigatePanel.NavigatePanelNode CreateNode()
        {
            // should set nodeRef
            System.Diagnostics.Debugger.Break();
            return NavigatePanelNode;
        }

        public virtual NavigatePanel.NavigatePanelNode? CreateLinkNode()
        {
            NavigatePanel.NavigatePanelNode node;
            node = CreateNode();
            return node;
        }

        public virtual DocumentParser? CreateDocumentParser(DocumentParser.ParseModeEnum parseMode,System.Threading.CancellationToken? token)
        {
            return null;
        }


    }
}
