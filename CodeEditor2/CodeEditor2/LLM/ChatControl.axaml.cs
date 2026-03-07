using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.AI;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static CodeEditor2.LLM.ChatControl;
using static SkiaSharp.HarfBuzz.SKShaper;
using static System.Net.Mime.MediaTypeNames;

namespace CodeEditor2.LLM;

public partial class ChatControl : UserControl
{
    public ChatControl()
    {
        DataContext = this;
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            return;
        }
        Items.Add(inputItem);

        var keyBinding = new KeyBinding
        {
            Gesture = new KeyGesture(Key.Enter, KeyModifiers.None),
            Command = ReactiveCommand.Create(() =>
            {
                inputItem.SendButton.Focus();
            }),
        };
        inputItem.TextBox.KeyBindings.Add(keyBinding);
        inputItem.SendButton.Click += SendButton_Click;
        inputItem.SaveButton.Click += SaveButton_Click;
        inputItem.ClearButton.Click += ClearButton_Click;
        inputItem.LoadButton.Click += LoadButton_Click;
        inputItem.AbortButton.Click += AbortButton_Click;
        inputItem.TextBox.Focus();
        ListBox0.Loaded += ListBox0_Loaded;
    }


    // initialize
    private ScrollViewer scrollViewer;
    private bool _isInternalScrolling = false; // システムによるスクロール中かどうかのフラグ
    private bool initialized = false;
    private void ListBox0_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var scrollViewer = ListBox0.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null) throw new Exception();

        scrollViewer = ListBox0.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null) return;

        scrollViewer.GetObservable(ScrollViewer.ExtentProperty).Subscribe(_ =>
        {
            if (autoScroll)
            {
                _isInternalScrolling = true; // 「今からシステムが動かします」という合図
                scrollViewer.Offset = new Vector(scrollViewer.Offset.X, double.MaxValue);

                // 描画サイクルが終わる頃にフラグを下ろす
                Dispatcher.UIThread.Post(() => _isInternalScrolling = false, DispatcherPriority.Loaded);
            }
        });

        // 2. スクロール位置が変わった時の処理
        scrollViewer.ScrollChanged += (s, ev) =>
        {
            // システムによるスクロール（Extent変化による移動）なら、手動判定をスルーする
            if (_isInternalScrolling) return;

            // 垂直方向の移動がない場合は無視
            //            if (Math.Abs(ev.ExtentDelta.Length) < 0.1) return;

            // 「一番下に近いか」を判定
            const double threshold = 30; // 少し余裕を持たせる
            bool isAtBottom = scrollViewer.Offset.Y >= (scrollViewer.Extent.Height - scrollViewer.Viewport.Height - threshold);

            if (!isAtBottom)
            {
                // ユーザーが手動で上に上げた
                autoScroll = false;
            }
            else
            {
                // ユーザーが自力で一番下まで戻した
                autoScroll = true;
            }
        };
        this.scrollViewer = scrollViewer;
        if(!initialized)
        {
            initialized = true;
            Dispatcher.UIThread.Post(async () =>
            {
                await ResetAsync();
            });
        }
        //await ResetAsync();
    }

    public ObservableCollection<ChatItem> Items { get; set; } = new ObservableCollection<ChatItem>();

    private ILLMChatFrontEnd? chat { get; set; } = null;
    private LLMAgent? agent { get; set; } = null;
    private bool autoScroll { get; set; } = true;

    public bool AutoSave { set; get; } = false;
    public string? LogFilePath { set; get; } = null;

    InputItem inputItem = new InputItem();
    CollapsibleTextItem? lastResultItem = null;

    private Avalonia.Media.Color commandColor = new Avalonia.Media.Color(255, 255, 200, 200);
    private Avalonia.Media.Color completeColor = new Avalonia.Media.Color(255, 200, 255, 255);

    bool inputAcceptable = true;

    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();


    // external control
    // set model and agent
    public void SetModel(ILLMChatFrontEnd chatModel, LLMAgent? agent)
    {
        this.chat = chatModel;
        this.agent = agent;
        if (initialized)
        {
            initialized = true;
            Dispatcher.UIThread.Post(async () =>
            {
                await ResetAsync();
            });
        }
    }
    public async Task ResetAsync()
    {
        if (chat == null) return;
        await chat.ResetAsync();

        // remove items
        int itemCount = Items.Count;
        for (int i = 0; i < itemCount - 2; i++)
        {
            Items.RemoveAt(1);
        }

        cancellationTokenSource = new CancellationTokenSource();
        // restart
        if (agent != null)
        {
            string basePrompt = await agent.GetBasePromptAsync(cancellationTokenSource.Token);
            if (basePrompt == "") return;
            await UserComplete(basePrompt,true);
        }
    }

    private async Task UserComplete(string command,bool collapse = false)
    {
        if (!inputAcceptable) return;
        cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        hashHistory.Clear();

        // get toools
        IList<AITool>? tools = null;
        if (agent != null)
        {
            tools = agent.Tools;
            if (tools.Count == 0) tools = null;
        }
        await completeWithFunctionCall(command, tools, cancellationToken, collapse);
    }
    private string getHash(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        byte[] hashBytes = XxHash64.Hash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private List<string> hashHistory = new List<string>();

    private async Task completeWithFunctionCall(
        string command, IList<AITool>? tools,
        CancellationToken cancellationToken,
        bool collapseQuestion
        )
    {
        string? result;
        bool loopAlarted = false;
        string loopedHash = "";

        hashHistory.Add(getHash(command));
        result = await complete(command, tools, cancellationToken, collapseQuestion);
        if (result == null) return;
        hashHistory.Add(getHash(result));

        while (agent !=null && !cancellationToken.IsCancellationRequested)
        {
            string? functioncallCommand = await agent.ParseResponceAsync(result, cancellationToken);
            if (functioncallCommand == null) return;

            hashHistory.Add(getHash(functioncallCommand));
            result = await complete(functioncallCommand, tools, cancellationToken, true);
            if (result == null) return;
            hashHistory.Add(getHash(result));
            if (isLoopDetected())
            {
                if(loopAlarted && hashHistory.Last() == loopedHash)
                {
                    return;
                }
                loopAlarted = true;
                loopedHash = hashHistory.Last();
                command = "あなたはループに陥っています。別の視点で考えてください";
                hashHistory.Add(getHash(command));
                result = await complete(functioncallCommand, tools, cancellationToken, true);
            }
        }

    }

    // ループとみなす最小の周期（例：2なら A->B->A->B を検出）
    private const int MinPatternLength = 2;
    // ループとみなす繰り返しの回数（例：3なら 同じパターンが3回続いたらループ）
    private const int MaxRepetitions = 2;
    private bool isLoopDetected()
    {
        if (hashHistory.Count < (MinPatternLength * MaxRepetitions))
            return false;

        // 2. パターン長を変化させて検証 (1つ戻る、2つ戻る...)
        // 直近のn件のシーケンスが、その前のn件と一致するかをチェックします
        for (int patternSize = 1; patternSize <= hashHistory.Count / MaxRepetitions; patternSize++)
        {
            var currentPattern = hashHistory.TakeLast(patternSize).ToList();
            bool isRepeating = true;

            for (int i = 1; i < MaxRepetitions; i++)
            {
                var previousPattern = hashHistory
                    .Skip(hashHistory.Count - (patternSize * (i + 1)))
                    .Take(patternSize)
                    .ToList();

                if (!currentPattern.SequenceEqual(previousPattern))
                {
                    isRepeating = false;
                    break;
                }
            }

            if (isRepeating)
            {
                // ループ検出！
                return true;
            }
        }

        return false;
    }

    private async Task<string?> complete(
        string command, IList<AITool>? tools, 
        CancellationToken cancellationToken,
        bool collapseQuestion
        )
    {
        if (chat == null) return null;
        // reentrant lock
        inputAcceptable = false;

        try
        {
            CollapsibleTextItem commandItem = new CollapsibleTextItem(command);
            inputItem.TextBox.Text = "";
            commandItem.TextColor = commandColor;
            Items.Insert(Items.Count - 1, commandItem);
            if (collapseQuestion) commandItem.Collapsed = true;

            CollapsibleTextItem resultItem = new CollapsibleTextItem("");
            Items.Insert(Items.Count - 1, resultItem);
            lastResultItem = resultItem;

            // show progress timer
            var stopwatch = Stopwatch.StartNew();

            bool timerActivate = true; // timer activate flag, timer will be stopped when first result is returned
            using var timerCancellationTokenSource = new CancellationTokenSource();
            var displayTimerTask = Task.Run(async () =>
            {
                while (!timerCancellationTokenSource.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (timerActivate)
                        {
                            await resultItem.SetText("waiting ... " + (stopwatch.ElapsedMilliseconds / 1000f).ToString("F1") + "s");
                        }
                        await Task.Delay(100, timerCancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, timerCancellationTokenSource.Token);

            cancellationToken.ThrowIfCancellationRequested();
            // execute chat command
            try
            {
                await foreach (string ret in chat.GetAsyncCollectionChatResult(command, tools, cancellationToken))
                {
                    if (timerActivate & ret != "")
                    {
                        timerCancellationTokenSource.Cancel();
                        timerActivate = false;
                        await resultItem.SetText("");
                    }
                    await resultItem.AppendText(ret);
                    
                    // 1トークンごとにループをチェック
                    if (IsStreamingLoopDetected(resultItem.Text))
                    {
                        await resultItem.AppendText("\n[Loop detected: Generation stopped.]");
                        break; // foreachを抜けて生成を中断
                    }

                    if (autoScroll)
                    {
                        // 描画スレッドで即座にレイアウトを確定させる
                        Dispatcher.UIThread.Post(() =>
                        {
                            ListBox0.UpdateLayout();
                            scrollViewer?.ScrollToEnd();
                        }, DispatcherPriority.Render); // Render優先度で即座に反映
                    }
                }
                if (timerActivate)
                {
                    timerCancellationTokenSource.Cancel();
                    timerActivate = false;
                    await resultItem.SetText("blank");
                }
            }
            catch (OperationCanceledException ex)
            {
                
            }
            catch (Exception ex)
            {
                CodeEditor2.Controller.AppendLog(ex.Message, Avalonia.Media.Colors.Red);
            }
            await displayTimerTask;
            stopwatch.Stop();

            // change color to complete color
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                resultItem.TextColor = completeColor;
                await SaveMessagesAsync();
            });

            string result = resultItem.Text;
            return result;
        }
        catch (Exception ex)
        {
            CodeEditor2.Controller.AppendLog(ex.Message, Avalonia.Media.Colors.Red);
        }
        finally
        {
            inputAcceptable = true;
        }
        return null;
    }

    private bool IsStreamingLoopDetected(string fullText)
    {
        const int minPatternLength = 4; // 短すぎる一致（"ABC"など）は無視
        const int repeatCount = 3;      // 3回連続でループと判定

        if (string.IsNullOrEmpty(fullText) || fullText.Length < minPatternLength * repeatCount)
            return false;

        // 後ろからパターンサイズを広げながらチェック
        for (int patternSize = minPatternLength; patternSize <= fullText.Length / repeatCount; patternSize++)
        {
            string pattern = fullText.Substring(fullText.Length - patternSize);

            // --- 改良ポイント: 記号や空白のみのパターンはループ判定から除外 ---
            // 文字列が「記号、数字、空白」だけで構成されている場合はスキップ
            // (例: "    ", "----", "////", "1212")
            if (!Regex.IsMatch(pattern, @"[\p{L}\p{IsCJKUnifiedIdeographs}]"))
            {
                continue;
            }

            bool allMatch = true;
            for (int i = 1; i < repeatCount; i++)
            {
                int start = fullText.Length - (patternSize * (i + 1));
                if (fullText.Substring(start, patternSize) != pattern)
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch) return true;
        }
        return false;
    }
    public async IAsyncEnumerable<string> GetAsyncCollectionChatResult(string command, IList<AITool>? tools, [EnumeratorCancellation] CancellationToken cancellation)
    {
        inputItem.TextBox.Text = command;
        await complete(command, tools, cancellation,false);
        if (lastResultItem == null) yield break;
        yield return lastResultItem.Text;
    }
    public async Task<string> GetAsyncChatResult(string command, IList<AITool>? tools, CancellationToken cancellationToken)
    {
        StringBuilder sb = new StringBuilder();
        await foreach (string ret in GetAsyncCollectionChatResult(command, tools, cancellationToken))
        {
            sb.Append(ret);
        }
        return sb.ToString();
    }

    // save & load ////////////////////

    public async Task LoadMessagesAsync(string filePath)
    {
        LogFilePath = filePath;
        await LoadMessagesAsync();
    }
    public async Task LoadMessagesAsync()
    {
        if (chat == null) return;
        if (LogFilePath == null) return;
        await chat.LoadMessagesAsync(LogFilePath);

        int items = Items.Count;
        for (int i = 0; i < items - 2; i++)
        {
            Items.RemoveAt(1);
        }

        List<Microsoft.Extensions.AI.ChatMessage> chatmessages = chat.GetChatMessages();
        foreach (Microsoft.Extensions.AI.ChatMessage chatmessage in chatmessages)
        {
            CollapsibleTextItem resultItem = new CollapsibleTextItem(chatmessage.Text);
            if (chatmessage.Role == ChatRole.System)
            {
                resultItem.TextColor = completeColor;
            }
            else if (chatmessage.Role == ChatRole.User)
            {
                resultItem.TextColor = commandColor;
            }
            else if (chatmessage.Role == ChatRole.Assistant)
            {
                resultItem.TextColor = completeColor;
            }
            else if (chatmessage.Role == ChatRole.Tool)
            {
                resultItem.TextColor = completeColor;
            }
            Items.Insert(Items.Count - 1, resultItem);
            lastResultItem = resultItem;
        }
    }
    public async Task SaveMessagesAsync(string filePath)
    {
        LogFilePath = filePath;
        await SaveMessagesAsync();
    }
    private async Task SaveMessagesAsync()
    {
        if (chat == null) return;
        if (LogFilePath == null) return;
        await chat.SaveMessagesAsync(LogFilePath);
    }


    // ui control
    private void AbortButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        cancellationTokenSource.Cancel();
    }
    private async void LoadButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () => await LoadMessagesAsync());
        }
        catch (Exception exception)
        {
            CodeEditor2.Controller.AppendLog(exception.Message, Avalonia.Media.Colors.Red);
        }
    }
    private void ClearButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            Dispatcher.UIThread.Invoke(async () =>
            {
                await ResetAsync();
            });
        }
        catch (Exception exception)
        {
            CodeEditor2.Controller.AppendLog(exception.Message, Avalonia.Media.Colors.Red);
        }
    }

    private async void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () => await SaveMessagesAsync());
        }
        catch (Exception exception)
        {
            CodeEditor2.Controller.AppendLog(exception.Message, Avalonia.Media.Colors.Red);
        }
    }

    private async void SendButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            string? command = inputItem.TextBox.Text;
            if (command == null) return;

            await UserComplete(command);

            inputItem.TextBox.Focus();
        }
        catch (Exception exception)
        {
            CodeEditor2.Controller.AppendLog(exception.Message, Avalonia.Media.Colors.Red);
        }
    }

}

