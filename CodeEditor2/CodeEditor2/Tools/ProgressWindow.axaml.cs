using Avalonia.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static CodeEditor2.CodeEditor.ParsedDocument;

namespace CodeEditor2.Tools
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
            Title = "Title";
            Message = "Loading...";
            ProgressMaxValue = 100;
        }

        public ProgressWindow(string title,string message,double max)
        {
            InitializeComponent();
            Title = title;
            Message = message;
            ProgressMaxValue = max;
        }


        private string _Title;
        public string Titile
        {
            get { return _Title; }
            set { 
                _Title = value;
                TitleText.Text = _Title;
                TitleText.InvalidateVisual();
            }
        }

        private string _Message;
        public string Message
        {
            get { return _Message; }
            set
            {
                _Message = value;
                MessageText.Text = _Message;
                MessageText.InvalidateVisual();
            }
        }

        private double _ProgressMaxValue;
        public double ProgressMaxValue
        {
            get
            {
                return _ProgressMaxValue;
            }
            set
            {
                _ProgressMaxValue = value;
                ProgressBar0.Maximum = _ProgressMaxValue;
                ProgressBar0.InvalidateVisual();
            }
        }

        private double _ProgressValue = 0;
        public double ProgressValue
        {
            get
            {
                return _ProgressValue;
            }
            set
            {
                _ProgressValue = value;
                ProgressBar0.Value = _ProgressValue;
                ProgressBar0.InvalidateVisual();
            }
        }

    }
}
