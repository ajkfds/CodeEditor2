using Avalonia.Controls;
using CodeEditor2.MessageView;
using System.Security.Cryptography.X509Certificates;
using System.Collections.ObjectModel;
using Avalonia.Styling;
using Avalonia.Layout;
using System.Linq;

namespace CodeEditor2.Views
{
    public partial class InfoView : UserControl
    {
        public InfoView()
        {
            InitializeComponent();
            Global.infoView = this;

            InfoListBox0.DataContext = this;
            InfoListBox0.ItemsSource = Items;

            Style style = new Style();
            style.Selector = ((Selector?)null).OfType(typeof(ListBoxItem));
            style.Add(new Setter(Layoutable.MinHeightProperty, 8.0));
//            style.Add(new Setter(Layoutable.HeightProperty, 11.0));

            InfoListBox0.Styles.Add(style);
        }

        ObservableCollection<ListBoxItem> Items = new ObservableCollection<ListBoxItem>();

        //public void MessageView_NodeClicked(AjkAvaloniaLibs.Contorls.TreeNode node)
        //{
        //    MessageNode _node = node as MessageNode;
        //    if (node == null) return;
        //    node.OnSelected();
        //}

        public void UpdateMessages(CodeEditor.ParsedDocument parsedDocument)
        {

            lock (Items)
            {
                Items.Clear();

                if (parsedDocument != null)
                {
                    foreach (CodeEditor.ParsedDocument.Message message in parsedDocument.Messages.ToList())
                    {
                        Items.Add(message.CreateMessageNode().ListBoxItem());
                    }
                }
            }

            InfoListBox0.InvalidateVisual();
        }

    }
}
