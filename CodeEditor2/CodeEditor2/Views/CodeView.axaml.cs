using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Resources;
using System.Security.Cryptography.X509Certificates;

using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Diagnostics;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using System.Reflection.Emit;
using System.Diagnostics.CodeAnalysis;
using CodeEditor2.CodeEditor;
using System.Threading;
using Avalonia.Threading;
using System.Diagnostics;

namespace CodeEditor2.Views
{
    public partial class CodeView : UserControl
    {
        private readonly TextEditor _textEditor;
        private FoldingManager _foldingManager;
        private readonly TextMate.Installation _textMateInstallation;
        private CompletionWindow _completionWindow;
        private OverloadInsightWindow _insightWindow;
        private TextMateSharp.Grammars.RegistryOptions _registryOptions;
        private int _currentTheme = (int)ThemeName.DarkPlus;

        private BackroungParser backGroundParser = new BackroungParser();


        public CodeView()
        {
            InitializeComponent();

            Global.codeView = this;

            _textEditor = this.FindControl<TextEditor>("Editor");
            _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
            _textEditor.Background = Brushes.Transparent;
            _textEditor.ShowLineNumbers = true;
            _textEditor.Options.ShowTabs = true;
            _textEditor.Options.ShowSpaces = true;
            _textEditor.Options.EnableImeSupport = true;
            _textEditor.Options.ShowEndOfLine = true;
            _textEditor.Options.ShowColumnRulers = true;



            _textEditor.ContextMenu = new ContextMenu
            {
                ItemsSource = new List<MenuItem>
                {
                    new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) },
                    new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) },
                    new MenuItem { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) }
                }
            };
            _textEditor.TextArea.Background = this.Background;
            _textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
            _textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
            _textEditor.TextArea.DocumentChanged += TextArea_DocumentChanged;
//            _textEditor.TextArea.


            _textEditor.Options.ShowBoxForControlCharacters = true;
            _textEditor.Options.ColumnRulerPositions = new List<int>() { 80, 100 };
            _textEditor.TextArea.IndentationStrategy = new AvaloniaEdit.Indentation.CSharp.CSharpIndentationStrategy(_textEditor.Options);
            _textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
//            _textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            _textEditor.TextArea.RightClickMovesCaret = true;

//            _changeThemeButton = this.FindControl<Button>("changeThemeBtn");
//            _changeThemeButton.Click += ChangeThemeButton_Click;

//            _textEditor.TextArea.TextView.ElementGenerators.Add(_generator);

            //_registryOptions = new TextMateSharp.Grammars.RegistryOptions(
            //    (ThemeName)_currentTheme);

//            _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);

//            Language csharpLanguage = _registryOptions.GetLanguageByExtension(".cs");

//            _syntaxModeCombo = this.FindControl<ComboBox>("syntaxModeCombo");
//            _syntaxModeCombo.ItemsSource = _registryOptions.GetAvailableLanguages();
//            _syntaxModeCombo.SelectedItem = csharpLanguage;
//            _syntaxModeCombo.SelectionChanged += SyntaxModeCombo_SelectionChanged;

//            string scopeName = _registryOptions.GetScopeByLanguageId(csharpLanguage.Id);
            
            _textEditor.Document = new TextDocument(
                "// AvaloniaEdit supports displaying control chars: \a or \b or \v" + Environment.NewLine +
                "// AvaloniaEdit supports displaying underline and strikethrough" + Environment.NewLine);
            //+ ResourceLoader.LoadSampleFile(scopeName));
//            _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
            _textEditor.TextArea.TextView.LineTransformers.Add(new CodeDocumentColorTransformer());

//            _statusTextBlock = this.Find<TextBlock>("StatusText");

            this.AddHandler(PointerWheelChangedEvent, (o, i) =>
            {
                if (i.KeyModifiers != KeyModifiers.Control) return;
                if (i.Delta.Y > 0) _textEditor.FontSize++;
                else _textEditor.FontSize = _textEditor.FontSize > 1 ? _textEditor.FontSize - 1 : 1;
            }, RoutingStrategies.Bubble, true);

            backGroundParser.Run();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Tick += Timer_Tick;
            timer.Start();

            for(byte i = 0; i < 16; i++)
            {
                SolodColorBrushes.Add(i, new SolidColorBrush(Global.DefaultDrawStyle.ColorPallet[i]));
            }
        }

        private void TextArea_DocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
//            System.Diagnostics.Debug.Print("## TextArea_DocumentChanged");
        }

        int prevCarletLine = 0;
        ulong prevVersion = 0;
        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
//            System.Diagnostics.Debug.Print("## Caret_PositionChanged");
            if (CodeDocument == null) return;
            int carletLine = _textEditor.TextArea.Caret.Line;
            ulong version = codeDocument.Version;
            Debug.Print("version "+version.ToString()+"  carletLine"+carletLine.ToString());
            if(prevVersion != version && carletLine != prevCarletLine)
            {
                entryParse();
            }
            prevCarletLine = carletLine;
            prevVersion = version;
        }

        public static Dictionary<byte, SolidColorBrush> SolodColorBrushes = new Dictionary<byte, SolidColorBrush>();


        private void Timer_Tick(object? sender, EventArgs e)
        {
            DocumentParser parser = backGroundParser.GetResult();
            if (parser == null) return;
            if (parser.ParsedDocument == null) return;
            //if (TextFile == null) return;
            //if (TextFile != parser.TextFile)
            //{   // return not current file
            //    return;
            //}

            Data.TextFile textFile = parser.TextFile;
            CodeDocument codeDocument = textFile.CodeDocument;

            if (textFile == null || textFile == null)
            {
                parser.Dispose();
                return;
            }

            Controller.AppendLog("complete edit parse ID :" + parser.TextFile.ID);
            if (codeDocument.Version != parser.ParsedDocument.Version)
            {
                Controller.AppendLog("edit parsed mismatch " + DateTime.Now.ToString() + "ver" + codeDocument.Version + "<-" + parser.ParsedDocument.Version);
                parser.Dispose();
                return;
            }

            //            CodeDocument.CopyFrom(parser.Document);
            codeDocument.CopyColorMarkFrom(parser.Document);

            if (parser.ParsedDocument != null)
            {
                parser.TextFile.AcceptParsedDocument(parser.ParsedDocument);
            }

            // update current view
            //            codeTextbox.Invoke(new Action(codeTextbox.Refresh));
            //            Controller.MessageView.Update(TextFile.ParsedDocument);
            //            codeTextbox.ReDrawHighlight();

            //            Controller.NavigatePanel.UpdateVisibleNode();
            //            Controller.NavigatePanel.Refresh();
            _textEditor.TextArea.TextView.Redraw();
        }

        public void SetTextFile(Data.TextFile textFile)
        {
            if (textFile == null)
            {
//                Global.mainForm.editorPage.CodeEditor.SetTextFile(null);
//                Global.mainForm.mainTab.TabPages[0].Text = "-";
            }
            else
            {
                CodeDocument = textFile.CodeDocument;
                
                //                Global.mainForm.editorPage.CodeEditor.AbortInteractiveSnippet();
                //                Global.mainForm.editorPage.CodeEditor.SetTextFile(textFile);
                //                Global.mainForm.mainTab.TabPages[0].Text = textFile.Name;
                //                Global.mainForm.mainTab.SelectedTab = Global.mainForm.mainTab.TabPages[0];
            }

            //if (TextFile != null)
            //{
            //    if (closeCantidateTextFiles.Contains(textFile))
            //    {
            //        closeCantidateTextFiles.Remove(textFile);
            //    }
            //    closeCantidateTextFiles.Add(textFile);
            //    if (closeCantidateTextFiles.Count > FilesCasheNumbers)
            //    {
            //        closeCantidateTextFiles[0].Close();
            //        closeCantidateTextFiles.RemoveAt(0);
            //    }
            //}

            if (textFile == null || textFile.CodeDocument == null)
            {
                CodeDocument = null;
                //codeTextbox.Visible = false;
                return;
            }
            if (TextFile == null || TextFile.GetType() != textFile.GetType())
            {
                //codeTextbox.Style = textFile.DrawStyle;
            }

            //codeTextbox.Visible = true;
//            codeTextbox.Document = textFile.CodeDocument;
//            TextFile = textFile;
            ScrollToCaret();
//            if (TextFile != null) Controller.MessageView.Update(TextFile.ParsedDocument);

            entryParse();

        }


        public void ScrollToCaret()
        {
            _textEditor.ScrollToLine(CodeDocument.GetLineAt(_textEditor.CaretOffset));
        }

        CodeEditor.CodeDocument codeDocument = null;
        public CodeEditor.CodeDocument CodeDocument
        {
            get
            {
                return codeDocument;
            }
            set
            {
                codeDocument = value;
                if(codeDocument == null)
                {
//                    _textEditor.Document = null;
                }
                else
                {
                    _textEditor.Document = codeDocument.TextDocument;
                }
            }
        }

        public Data.TextFile? TextFile
        {
            get
            {
                if (codeDocument == null) return null;
                return codeDocument.TextFile;
            }
        }


        class CodeDocumentColorTransformer : DocumentColorizingTransformer
        {
            protected override void ColorizeLine(DocumentLine line)
            {
                
                if (Global.mainView.CodeView.CodeDocument == null) return;

                CodeDocument codeDocument = Global.mainView.CodeView.CodeDocument;
                if (!codeDocument.LineInfomations.ContainsKey(line.LineNumber)) return;
                CodeEditor.LineInfomation lineInfo = codeDocument.LineInfomations[line.LineNumber];

                foreach(var color in lineInfo.Colors)
                {
                    if (line.Offset > color.Offset | color.Offset + color.Length > line.EndOffset) continue;
                    ChangeLinePart(
                        color.Offset,
                        color.Offset+color.Length,
                        visualLine =>
                        {
                            visualLine.TextRunProperties.SetForegroundBrush(SolodColorBrushes[color.ColorIndex]);
                        }
                    );

                }


                //SolidColorBrush cb = new SolidColorBrush(Color.FromRgb(255, 0, 0), 10.5);

                //string lineText = this.CurrentContext.Document.GetText(line);

                //int indexOfUnderline = lineText.IndexOf("underline");
                //if (indexOfUnderline == -1) return;
                //ChangeLinePart(
                //    line.Offset + indexOfUnderline,
                //    line.Offset + indexOfUnderline + "underline".Length,
                //    visualLine =>
                //    {
                //        visualLine.TextRunProperties.SetForegroundBrush(cb);
                //        if (visualLine.TextRunProperties.TextDecorations != null)
                //        {
                //            var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Underline[0] };

                //            visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                //        }
                //        else
                //        {
                //            visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                //        }
                //    }
                //);


            }

        }

        // -----------------------------------------------------------
        // Entry Edit Parse
        public void RequestReparse()
        {
            entryParse();
        }

        private void entryParse()
        {
//            if (Global.StopParse) return;
            if (TextFile == null) return;
            DocumentParser parser = TextFile.CreateDocumentParser(DocumentParser.ParseModeEnum.EditParse);
            if (parser != null)
            {
                Controller.AppendLog("entry edit parse ID :" + parser.TextFile.ID);
                backGroundParser.EntryParse(parser);
            }
        }

        // -----------------------------------------------------------

        private void textEditor_TextArea_TextEntering(object sender, TextInputEventArgs e)
        {
//            System.Diagnostics.Debug.Print("## TextArea_TextEntering");
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }

            _insightWindow?.Hide();

            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }

        private void textEditor_TextArea_TextEntered(object sender, TextInputEventArgs e)
        {
//            System.Diagnostics.Debug.Print("## TextArea_TextEntered");
            if (e.Text == ".")
            {

                _completionWindow = new CompletionWindow(_textEditor.TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow = null;

                var data = _completionWindow.CompletionList.CompletionData;
                data.Add(new MyCompletionData("Item1"));
                data.Add(new MyCompletionData("Item2"));
                data.Add(new MyCompletionData("Item3"));
                data.Add(new MyCompletionData("Item4"));
                data.Add(new MyCompletionData("Item5"));
                data.Add(new MyCompletionData("Item6"));
                data.Add(new MyCompletionData("Item7"));
                data.Add(new MyCompletionData("Item8"));
                data.Add(new MyCompletionData("Item9"));
                data.Add(new MyCompletionData("Item10"));
                data.Add(new MyCompletionData("Item11"));
                data.Add(new MyCompletionData("Item12"));
                data.Add(new MyCompletionData("Item13"));


                _completionWindow.Show();
            }
            else if (e.Text == "(")
            {
                _insightWindow = new OverloadInsightWindow(_textEditor.TextArea);
                _insightWindow.Closed += (o, args) => _insightWindow = null;

                _insightWindow.Provider = new MyOverloadProvider(new[]
                {
                    ("Method1(int, string)", "Method1 description"),
                    ("Method2(int)", "Method2 description"),
                    ("Method3(string)", "Method3 description"),
                });

                _insightWindow.Show();
            }else if(e.Text == "\n")
            {
                entryParse();
            }
        }

        //class UnderlineAndStrikeThroughTransformer : DocumentColorizingTransformer
        //{
        //    protected override void ColorizeLine(DocumentLine line)
        //    {
        //        SolidColorBrush cb = new SolidColorBrush(Color.FromRgb(255, 0, 0), 10.5);

        //        string lineText = this.CurrentContext.Document.GetText(line);

        //        int indexOfUnderline = lineText.IndexOf("underline");
        //        if (indexOfUnderline == -1) return;
        //        ChangeLinePart(
        //            line.Offset + indexOfUnderline,
        //            line.Offset + indexOfUnderline + "underline".Length,
        //            visualLine =>
        //            {
        //                visualLine.TextRunProperties.SetForegroundBrush(cb);
        //                if (visualLine.TextRunProperties.TextDecorations != null)
        //                {
        //                    var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Underline[0] };

        //                    visualLine.TextRunProperties.SetTextDecorations(textDecorations);
        //                }
        //                else
        //                {
        //                    visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
        //                }
        //            }
        //        );


        //        //if (line.LineNumber == 2)
        //        //{
        //        //    string lineText = this.CurrentContext.Document.GetText(line);

        //        //    int indexOfUnderline = lineText.IndexOf("underline");
        //        //    int indexOfStrikeThrough = lineText.IndexOf("strikethrough");

        //        //    if (indexOfUnderline != -1)
        //        //    {
        //        //        ChangeLinePart(
        //        //            line.Offset + indexOfUnderline,
        //        //            line.Offset + indexOfUnderline + "underline".Length,
        //        //            visualLine =>
        //        //            {
        //        //                if (visualLine.TextRunProperties.TextDecorations != null)
        //        //                {
        //        //                    var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Underline[0] };

        //        //                    visualLine.TextRunProperties.SetTextDecorations(textDecorations);
        //        //                }
        //        //                else
        //        //                {
        //        //                    visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
        //        //                }
        //        //            }
        //        //        );
        //        //    }

        //        //    if (indexOfStrikeThrough != -1)
        //        //    {
        //        //        ChangeLinePart(
        //        //            line.Offset + indexOfStrikeThrough,
        //        //            line.Offset + indexOfStrikeThrough + "strikethrough".Length,
        //        //            visualLine =>
        //        //            {
        //        //                if (visualLine.TextRunProperties.TextDecorations != null)
        //        //                {
        //        //                    var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Strikethrough[0] };

        //        //                    visualLine.TextRunProperties.SetTextDecorations(textDecorations);
        //        //                }
        //        //                else
        //        //                {
        //        //                    visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough);
        //        //                }
        //        //            }
        //        //        );
        //        //    }
        //        //}
        //    }

        //}

        private class MyOverloadProvider : IOverloadProvider
        {
            private readonly IList<(string header, string content)> _items;
            private int _selectedIndex;

            public MyOverloadProvider(IList<(string header, string content)> items)
            {
                _items = items;
                SelectedIndex = 0;
            }

            public int SelectedIndex
            {
                get => _selectedIndex;
                set
                {
                    _selectedIndex = value;
                    OnPropertyChanged();
                    // ReSharper disable ExplicitCallerInfoArgument
                    OnPropertyChanged(nameof(CurrentHeader));
                    OnPropertyChanged(nameof(CurrentContent));
                    // ReSharper restore ExplicitCallerInfoArgument
                }
            }

            public int Count => _items.Count;
            public string CurrentIndexText => $"{SelectedIndex + 1} of {Count}";
            public object CurrentHeader => _items[SelectedIndex].header;
            public object CurrentContent => _items[SelectedIndex].content;

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class MyCompletionData : ICompletionData
        {
            public MyCompletionData(string text)
            {
                Text = text;
            }

            public IImage Image => null;

            public string Text { get; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content => Text;

            public object Description => "Description for " + Text;

            public double Priority { get; } = 0;

            public void Complete(TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, Text);
            }
        }


    }
}
