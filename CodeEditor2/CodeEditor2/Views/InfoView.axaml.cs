using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;

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

            //Style style = new Style();
            //style.Selector = ((Selector?)null).OfType(typeof(ListBoxItem));
            //style.Add(new Setter(Layoutable.MinHeightProperty, 8.0));
            //InfoListBox0.Styles.Add(style);
            // Wheel Scroll Implementation
            if (Global.ReducedRendering)
            {
                Avalonia.Media.RenderOptions.SetTextRenderingMode(InfoListBox0, Avalonia.Media.TextRenderingMode.Alias);
                var customFont = new Avalonia.Media.FontFamily(Global.ReducedRenderingCodeFontFamily);
                InfoListBox0.FontFamily = customFont;
            }
            AttachedToVisualTree += (s, e) =>
            {
                if (Global.ReducedRendering)
                {
                    double scale = this.VisualRoot?.RenderScaling ?? 1.0;
                    InfoListBox0.FontSize = System.Math.Ceiling((float)Global.ReducedRenderingFontSize / scale);
                }
            };

            this.AddHandler(PointerWheelChangedEvent, (o, i) =>
            {
                if (i.KeyModifiers != KeyModifiers.Control) return;
                if (i.Delta.Y > 0)
                {
                    InfoListBox0.FontSize = (int)InfoListBox0.FontSize + 1;
                }
                else
                {
                    InfoListBox0.FontSize = InfoListBox0.FontSize > 1 ? (int)InfoListBox0.FontSize - 1 : 1;
                }

            }, RoutingStrategies.Bubble, true);
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
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => { UpdateMessages(parsedDocument); });
                return;
            }


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
