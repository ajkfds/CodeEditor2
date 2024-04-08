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

namespace CodeEditor2.Views
{
    public partial class CodeView : UserControl
    {
        internal readonly TextEditor _textEditor;
        private FoldingManager _foldingManager;
        private readonly TextMate.Installation _textMateInstallation;
        internal AutoCompleteWindow _completionWindow;

        private OverloadInsightWindow _insightWindow;
        private TextMateSharp.Grammars.RegistryOptions _registryOptions;
        private int _currentTheme = (int)ThemeName.DarkPlus;


        public CodeView()
        {
            InitializeComponent();

            Global.codeView = this;

            codeViewPopup = new CodeViewPopup(this);
            codeViewParser = new CodeViewParser(this);
            Highlighter = new Highlighter(this);
            codeViewPopupMenu = new CodeViewPopupMenu(this);
            codeViewAutoComplete = new CodeViewAutoComplete(this);

            _textEditor = Editor;
            _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
            _textEditor.Background = Brushes.Transparent;
            _textEditor.ShowLineNumbers = true;
            _textEditor.Options.ShowTabs = true;
            _textEditor.Options.ShowSpaces = true;
            _textEditor.Options.EnableImeSupport = true;
            _textEditor.Options.ShowEndOfLine = true;
            _textEditor.Background = new SolidColorBrush(Color.FromRgb(10, 10, 10));

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


            _textEditor.Options.ShowBoxForControlCharacters = true;
            _textEditor.Options.ColumnRulerPositions = new List<int>() { 80, 100 };
            _textEditor.TextArea.IndentationStrategy = new AvaloniaEdit.Indentation.CSharp.CSharpIndentationStrategy(_textEditor.Options);
            _textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            _textEditor.TextArea.SelectionChanged += TextArea_SelectionChanged;
            _textEditor.TextArea.RightClickMovesCaret = true;

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

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Tick += Timer_Tick;
            timer.Start();
        }


        internal CodeViewPopup codeViewPopup;
        internal CodeViewParser codeViewParser;
        internal CodeViewPopupMenu codeViewPopupMenu;
        internal CodeViewAutoComplete codeViewAutoComplete;
        public Highlighter Highlighter;

        private void TextArea_KeyUp(object? sender, KeyEventArgs e)
        {
        }

        private void TextArea_PointerMoved(object? sender, PointerEventArgs e)
        {
            codeViewPopup.TextArea_PointerMoved(sender, e);
        }



        /// <summary>
        /// Called from textEditor
        /// </summary>
        int prevCarletLine = 0;
        ulong prevVersion = 0;
        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            if (CodeDocument == null) return;
            //CodeDocument.setCarletPosition(_textEditor.TextArea.Caret.Offset);
            CodeDocument.CaretIndex = _textEditor.TextArea.Caret.Offset;

            int carletLine = _textEditor.TextArea.Caret.Line;
            ulong version = codeDocument.Version;
            //Debug.Print("version "+version.ToString()+"  carletLine"+carletLine.ToString());
            if (prevVersion != version && carletLine != prevCarletLine)
            {
                codeViewParser.EntryParse();
            }
            prevCarletLine = carletLine;
            //            prevVersion = version;
        }

        private void TextArea_SelectionChanged(object? sender, EventArgs e)
        {
            if (CodeDocument == null) return;
            if (_textEditor.TextArea.Selection.Segments.Count() > 0)
            {
                CodeDocument.SelectionStart = _textEditor.TextArea.Selection.Segments.First().StartOffset;
                int offset;
                offset = _textEditor.TextArea.Selection.Segments.First().EndOffset;
                if (offset != 0) offset--;
                CodeDocument.SelectionLast = offset;
            }
        }

        // Called from CodeCdedocuent. Update CodeDocument Index change to textEditor
        private void CodeDocument_CarletChanged(CodeDocument codeDocument)
        {
            if (CodeDocument != codeDocument) return;

            // changed by CodeDocument Code
            if (_textEditor.CaretOffset == codeDocument.CaretIndex) return;
            _textEditor.CaretOffset = codeDocument.CaretIndex;
        }

        private void CodeDocument_SelectionStartChanged(CodeDocument codeDocument)
        {
            if (CodeDocument != codeDocument) return;
            //            if (_textEditor.CaretOffset == codeDocument.CaretIndex) return;
            //            _textEditor.CaretOffset = codeDocument.CaretIndex;
        }
        private void CodeDocument_SelectionLastChanged(CodeDocument codeDocument)
        {
            if (CodeDocument != codeDocument) return;
        }

        private void TextArea_DocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
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

        public void SetTextFile(Data.TextFile textFile)
        {
            if(CodeDocument != null)
            {
                CodeDocument.CaretChanged = null;
                CodeDocument.SelectionStartChanged = null;
                CodeDocument.SelectionLastChanged = null;
            }

            if (textFile == null)
            {
//                Global.mainForm.editorPage.CodeEditor.SetTextFile(null);
//                Global.mainForm.mainTab.TabPages[0].Text = "-";
            }
            else
            {
                CodeDocument = textFile.CodeDocument;
                CodeDocument.CaretChanged += CodeDocument_CarletChanged;
                CodeDocument.SelectionStartChanged += CodeDocument_SelectionStartChanged;
                CodeDocument.SelectionLastChanged += CodeDocument_SelectionLastChanged;

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
            //TextFile = textFile;
            ScrollToCaret();
            if (textFile != null) Controller.MessageView.Update(textFile.ParsedDocument);

            codeViewParser.EntryParse();
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



        // -----------------------------------------------------------
        // Entry Edit Parse
        public void RequestReparse()
        {
            codeViewParser.EntryParse();
        }

        // -----------------------------------------------------------

        private void TextArea_KeyDown(object? sender, KeyEventArgs e)
        {
            if(e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Shift)
            {
                e.Handled = true;
                codeViewPopupMenu.ShowToolSelectionPopupMenu();
            }
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

            TextFile?.TextEntering(e);
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }


        private void textEditor_TextArea_TextEntered(object? sender, TextInputEventArgs e)
        {
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
            codeViewAutoComplete.ForceOpenAutoComplete(autocompleteItems);
        }


    }
}
