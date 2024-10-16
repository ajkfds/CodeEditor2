using Avalonia.Media;
using HarfBuzzSharp;
using CodeEditor2.Data;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.NavigatePanel
{
    public class NavigatePanelNode : AjkAvaloniaLibs.Contorls.TreeNode
    {
        private void setImage()
        {
            Image =  AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2/Assets/Icons/document.svg",
                Avalonia.Media.Color.FromArgb(100, 100, 100, 100)
                );
        }
        public long ObjectID
        {
            get
            {
                bool firstTime;
                return Global.ObjectIDGenerator.GetId(this, out firstTime);
            }
        }

        protected NavigatePanelNode()
        {
            setImage();
        }

        public virtual void Refresh()
        {

        }

        public NavigatePanelNode(Item item)
        {
            itemRef = new WeakReference<Item>(item);
            Name = item.Name;
            if (NavigatePanelNodeCreated != null) NavigatePanelNodeCreated(this);
        }

        public new string Name { get; protected set; }

        private bool link = false;

        public bool Link
        {
            get
            {
                return link;
            }
            set
            {
                link = value;
            }
        }

        private WeakReference<Item> itemRef;
        public Item Item
        {
            get
            {
                Item ret;
                if (!itemRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        public static Action<NavigatePanelNode>? NavigatePanelNodeCreated;

        /// <summary>
        /// update this node and children
        /// </summary>
        public virtual void Update()
        {
        }

        public virtual void UpdateVisual()
        {
        }

        public virtual void Dispose()
        {

        }

        /// <summary>
        /// update all nodes under this node
        /// </summary>
        public virtual void HierarchicalUpdate()
        {
            HierarchicalUpdate(0);
        }

        public virtual void HierarchicalUpdate(int depth)
        {
            Update();
            if (depth > 100) return;
            foreach (NavigatePanelNode node in Nodes)
            {
                node.HierarchicalUpdate(depth + 1);
            }
        }
        public override void OnExpand()
        {
            HierarchicalVisibleUpdate();
        }

        public override void OnCollapse()
        {
            HierarchicalVisibleUpdate();
        }


        public virtual void HierarchicalVisibleUpdate()
        {
            HierarchicalVisibleUpdate(0, IsExpanded);
        }

        public virtual void HierarchicalVisibleUpdate(int depth, bool expanded)
        {
            Update();
            if (depth > 100) return;
            if (!expanded) return;
            foreach (NavigatePanelNode node in Nodes)
            {
                node.HierarchicalVisibleUpdate(depth + 1, node.IsExpanded);
            }
        }



        //public override void DrawNode(Graphics graphics, int x, int y, Font font, Color color, Color backgroundColor, Color selectedColor, int lineHeight, bool selected)
        //{
        //    if (Link) graphics.DrawImage(Global.IconImages.Link.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Blue), new Point(x, y));
        //    base.DrawNode(graphics, x, y, font, color, backgroundColor, selectedColor, lineHeight, selected);
        //}

        public virtual void ShowPropertyForm()
        {

        }

        public NavigatePanelNode GetRootNode()
        {
            NavigatePanelNode? parent = Parent as NavigatePanelNode;
            if (parent == null) return this;
            return parent.GetRootNode();
        }
    }
}
