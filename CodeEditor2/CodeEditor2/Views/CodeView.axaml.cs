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
using AjkAvaloniaLibs.Contorls;
using DynamicData.Binding;
using Avalonia.Controls.Primitives;
using System.Threading.Tasks;
using Avalonia.VisualTree;
using Avalonia.Rendering;
using CodeEditor2.Data;

namespace CodeEditor2.Views
{
    public partial class CodeView : UserControl
    {
        internal readonly TextEditor _textEditor;

        private FoldingManager? _foldingManager;
        private readonly TextMate.Installation? _textMateInstallation;
        internal AutoCompleteWindow? _completionWindow;
        private OverloadInsightWindow? _insightWindow;
        private TextMateSharp.Grammars.RegistryOptions? _registryOptions;

        private int _currentTheme = (int)ThemeName.DarkPlus;

        public CodeView()
        {
            InitializeComponent();

            Global.codeView = this;

            codeViewPopup = new CodeViewPopup(this);
            codeViewParser = new CodeViewParser(this);
//            Highlighter = new Highlighter(this);
            codeViewPopupMenu = new CodeViewPopupMenu(this);
            codeViewAutoComplete = new CodeViewAutoComplete(this);

            _textEditor = Editor;
            _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
            _textEditor.Background = Brushes.Transparent;
            _textEditor.ShowLineNumbers = true;

            _textEditor.Background = new SolidColorBrush(Color.FromRgb(10, 10, 10));

            _textEditor.Options.ShowTabs = true;
            _textEditor.Options.ShowSpaces = true;
            _textEditor.Options.EnableImeSupport = true;
            _textEditor.Options.ShowEndOfLine = true;
            _textEditor.Options.IndentationSize = 1;
            //            _textEditor.FontStyle.li

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
            _textEditor.TextArea.KeyDown += TextArea_KeyDown;
            _textEditor.TextArea.KeyUp += TextArea_KeyUp;


            _textEditor.TextArea.DocumentChanged += TextArea_DocumentChanged;
            _textEditor.TextArea.PointerMoved += TextArea_PointerMoved;
            _textEditor.Options.HighlightCurrentLine = true;

            _textEditor.Options.ShowBoxForControlCharacters = true;
            _textEditor.Options.ColumnRulerPositions = new List<int>() { 80, 100 };
//            _textEditor.TextArea.IndentationStrategy = new AvaloniaEdit.Indentation.CSharp.CSharpIndentationStrategy(_textEditor.Options);
            _textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            _textEditor.TextArea.SelectionChanged += TextArea_SelectionChanged;
            _textEditor.TextArea.RightClickMovesCaret = true;

            _highlightRenderer = new HighlightRenderer(new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)));
            _textEditor.TextArea.TextView.BackgroundRenderers.Add(_highlightRenderer);

            _markerRenderer = new MarkerRenderer();
            _textEditor.TextArea.TextView.BackgroundRenderers.Add(_markerRenderer);


            PopupMenu.Selected += PopupMenu_Selected;

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
                "// Press Ctrl + Space to force open auto-complete" + Environment.NewLine +
                "// Press Shit + Space to open quick tool menu" + Environment.NewLine);
            //+ ResourceLoader.LoadSampleFile(scopeName));
            //            _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(csharpLanguage.Id));

            _textEditor.TextArea.TextView.LineTransformers.Add(new CodeDocumentColorTransformer());

            this.AddHandler(PointerWheelChangedEvent, (o, i) =>
            {
                if (i.KeyModifiers != KeyModifiers.Control) return;
                if (i.Delta.Y > 0) _textEditor.FontSize++;
                else _textEditor.FontSize = _textEditor.FontSize > 1 ? _textEditor.FontSize - 1 : 1;
            }, RoutingStrategies.Bubble, true);

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        internal CodeEditor.HighlightRenderer _highlightRenderer;
        internal CodeEditor.MarkerRenderer _markerRenderer;
        internal CodeViewPopup codeViewPopup;
        internal CodeViewParser codeViewParser;
        internal CodeViewPopupMenu codeViewPopupMenu;
        internal CodeViewAutoComplete codeViewAutoComplete;
//        public Highlighter Highlighter;

        private void TextArea_KeyUp(object? sender, KeyEventArgs e)
        {
        }

        private void TextArea_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (skipEvents) return;
            codeViewPopup.TextArea_PointerMoved(sender, e);
        }



        /// <summary>
        /// Called from textEditor
        /// </summary>
        int prevCaretLine = 0;
        ulong prevVersion = 0;
        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            if (skipEvents) return;
            if (CodeDocument == null) return;
            CodeDocument.caretIndex = _textEditor.TextArea.Caret.Offset;

            int caretLine = _textEditor.TextArea.Caret.Line;
            ulong version = CodeDocument.Version;
            if (prevVersion != version && caretLine != prevCaretLine)
            {
                prevVersion = version;
                codeViewParser.EntryParse();
            }
            prevCaretLine = caretLine;
        }


        private void TextArea_SelectionChanged(object? sender, EventArgs e)
        {
            // mirror selection properties to CodeDocument

            if (skipEvents) return;
            if (CodeDocument == null) return;
            if (_textEditor.TextArea.Selection.Segments.Count() < 1) return;

            SelectionSegment segment;
            segment = _textEditor.TextArea.Selection.Segments.First();

            CodeDocument.selectionStart = segment.StartOffset;
            int offset;
            offset = segment.EndOffset;
            if (offset != 0) offset--;
            CodeDocument.selectionLast = offset;
        }

        // Called from CodeDocument. Update CodeDocument Index change to textEditor
        private void CodeDocument_CaretChanged(CodeDocument codeDocument)
        {
            if (skipEvents) return;
            if (CodeDocument != codeDocument) return;

            // changed by CodeDocument Code
            if (_textEditor.CaretOffset == codeDocument.CaretIndex) return;
            _textEditor.CaretOffset = codeDocument.CaretIndex;
        }

        public void SetCaretPosition(int index)
        {
            if (CodeDocument == null) return;
            if (_textEditor.CaretOffset == index && CodeDocument.caretIndex == index) return;
            _textEditor.CaretOffset = index;
            CodeDocument.caretIndex = index;
        }
        public void SetSelection(int selectionStart, int selectionLast)
        {
            if (skipEvents) return;
            if (CodeDocument != CodeDocument) return;

            _textEditor.TextArea.Selection = Selection.Create(_textEditor.TextArea, selectionStart, selectionLast + 1);
        }

        private void TextArea_DocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
            if (skipEvents) return;
            //            System.Diagnostics.Debug.Print("## TextArea_DocumentChanged");
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            codeViewParser.Timer_Tick(sender, e);
        }


        public void Redraw()
        {
            _textEditor.TextArea.TextView.Redraw();
        }

        private bool skipEvents = false;

        public void SetTextFile(Data.TextFile textFile,bool parseEntry)
        {
            skipEvents = true;

            System.Diagnostics.Debug.Print("## SetTextFile");
            if(CodeDocument != null)
            {
                detachFromCodeDocument();
            }

            if (textFile == null)
            {
//                Global.mainForm.editorPage.CodeEditor.SetTextFile(null);
//                Global.mainForm.mainTab.TabPages[0].Text = "-";
            }
            else
            {
                TextFile = textFile;
                attachToCodeDocument();
            }

            if (codeViewPopupMenu.Snippet != null) codeViewPopupMenu.AbortInteractiveSnippet();
            if (_completionWindow !=null && _completionWindow.IsVisible) _completionWindow.Hide();
            if (codeViewPopupMenu.IsOpened) codeViewPopupMenu.HidePopupMenu();

            if (textFile == null || textFile.CodeDocument == null)
            {
                TextFile = null;
                return;
            }
            
            //codeTextbox.Visible = true;
            //            codeTextbox.Document = textFile.CodeDocument;
            //TextFile = textFile;
            ScrollToCaret();
            if (textFile != null)
            {
                Controller.MessageView.Update(textFile.ParsedDocument);
                _textEditor.CaretOffset = textFile.CodeDocument.CaretIndex;
            }
            if (parseEntry) codeViewParser.EntryParse();

            skipEvents = false;

            Controller.CodeEditor.Refresh();
        }

        private void attachToCodeDocument()
        {
            //                CodeDocument.SelectionChanged += CodeDocument_SelectionChanged;

            //                Global.mainForm.editorPage.CodeEditor.AbortInteractiveSnippet();
            //                Global.mainForm.editorPage.CodeEditor.SetTextFile(textFile);
            //                Global.mainForm.mainTab.TabPages[0].Text = textFile.Name;
            //                Global.mainForm.mainTab.SelectedTab = Global.mainForm.mainTab.TabPages[0];
            TextFile.CodeDocument.Marks.CurrentMarks = _markerRenderer.marks;

            _markerRenderer.ClearMark();
            _markerRenderer.SetMarks(TextFile.CodeDocument.Marks.Details);
        }
        private void detachFromCodeDocument()
        {
            TextFile.CodeDocument.Marks.CurrentMarks = null;
        }

        public void ScrollToCaret()
        {
            if (CodeDocument == null) return;
            _textEditor.ScrollToLine(CodeDocument.GetLineAt(_textEditor.CaretOffset));
        }

        public CodeEditor.CodeDocument? CodeDocument
        {
            get
            {
                if (TextFile == null) return null;
                return TextFile.CodeDocument;
            }
        }

        private Data.TextFile? textFile;
        public Data.TextFile? TextFile
        {
            get
            {
                return textFile;
            }
            set
            {
                textFile = value;
                if(CodeDocument == null)
                {

                }
                else
                {
                    _textEditor.Document = CodeDocument.TextDocument;
                }
            }
        }



        // -----------------------------------------------------------
        // Entry Edit Parse
        public void RequestReparse()
        {
            codeViewParser.EntryParse();
        }

        // -----------------------------------------------------------

        private void TextArea_KeyDown(object? sender, KeyEventArgs e)
        {
             if (e.KeyModifiers == KeyModifiers.Control)
            {
                if(e.Key == Key.S)
                {
                    Controller.CodeEditor.Save();
                    e.Handled = true;
                    return;
                }

            }else if(e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Shift)
            {
                e.Handled = true;
                codeViewPopupMenu.ShowToolSelectionPopupMenu();
            }
            codeViewPopupMenu.TextArea_KeyDown(sender, e);
        }

        // tool selection form /////////////////////////////////////////////////////////////////////////



        //        public List<PopupMenuItem> PopupMenuItems = new List<PopupMenuItem>();

        public void OpenCustomSelection(List<CodeEditor2.CodeEditor.ToolItem> cantidates)
        {
            codeViewPopupMenu.OpenCustomSelection(cantidates);
        }


        public void HidePopupMenu()
        {
            codeViewPopupMenu.HidePopupMenu();
        }

        public void PopupMenu_Selected(PopupMenuItem popUpMenuItem)
        {
            codeViewPopupMenu.PopupMenu_Selected(popUpMenuItem);
        }

        public void StartInteractiveSnippet(Snippets.InteractiveSnippet interactiveSnippet)
        {
            codeViewPopupMenu.StartInteractiveSnippet(interactiveSnippet);
        }

        public void AbortInteractiveSnippet()
        {
            codeViewPopupMenu.AbortInteractiveSnippet();
        }



        private void textEditor_TextArea_TextEntering(object? sender, TextInputEventArgs e)
        {
            if (skipEvents) return;
            codeViewPopupMenu.TextEntering(sender, e);


            if (e.Text.Length > 0 && _completionWindow != null)
            {
                //if (!char.IsLetterOrDigit(e.Text[0]))
                //{
                //    // Whenever a non-letter is typed while the completion window is open,
                //    // insert the currently selected element.
                //    _completionWindow.CompletionList.RequestInsertion(e);
                //}
            }

            _insightWindow?.Hide();

            TextFile?.TextEntering(e);
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }


        private void textEditor_TextArea_TextEntered(object? sender, TextInputEventArgs e)
        {
            if (skipEvents) return;
            codeViewPopupMenu.TextEntered(sender, e);
            if (e.Text == "\n")
            {
                codeViewParser.EntryParse();
                return;
            }
            codeViewAutoComplete.CheckAutoComplete();
            TextFile?.TextEntered(e);
        }

        public void ForceOpenAutoComplete(List<AutocompleteItem> autocompleteItems)
        {
            if (skipEvents) return;
            codeViewAutoComplete.ForceOpenAutoComplete(autocompleteItems);
        }


    }
}
