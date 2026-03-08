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
    private bool _isInternalScrolling = false; // 繧ｷ繧ｹ繝・Β縺ｫ繧医ｋ繧ｹ繧ｯ繝ｭ繝ｼ繝ｫ荳ｭ縺九←縺・°縺ｮ繝輔Λ繧ｰ
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
                _isInternalScrolling = true; // 縲御ｻ翫°繧峨す繧ｹ繝・Β縺悟虚縺九＠縺ｾ縺吶阪→縺・≧蜷亥峙
                scrollViewer.Offset = new Vector(scrollViewer.Offset.X, double.MaxValue);

                // 謠冗判繧ｵ繧､繧ｯ繝ｫ縺檎ｵゅｏ繧矩・↓繝輔Λ繧ｰ繧剃ｸ九ｍ縺・
                Dispatcher.UIThread.Post(() => _isInternalScrolling = false, DispatcherPriority.Loaded);
            }
        });

        // 2. 繧ｹ繧ｯ繝ｭ繝ｼ繝ｫ菴咲ｽｮ縺悟､峨ｏ縺｣縺滓凾縺ｮ蜃ｦ逅・
        scrollViewer.ScrollChanged += (s, ev) =>
        {
            // 繧ｷ繧ｹ繝・Β縺ｫ繧医ｋ繧ｹ繧ｯ繝ｭ繝ｼ繝ｫ・・xtent螟牙喧縺ｫ繧医ｋ遘ｻ蜍包ｼ峨↑繧峨∵焔蜍募愛螳壹ｒ繧ｹ繝ｫ繝ｼ縺吶ｋ
            if (_isInternalScrolling) return;

            // 蝙ら峩譁ｹ蜷代・遘ｻ蜍輔′縺ｪ縺・ｴ蜷医・辟｡隕・
            //            if (Math.Abs(ev.ExtentDelta.Length) < 0.1) return;

            // 縲御ｸ逡ｪ荳九↓霑代＞縺九阪ｒ蛻､螳・
            const double threshold = 30; // 蟆代＠菴呵｣輔ｒ謖√◆縺帙ｋ
            bool isAtBottom = scrollViewer.Offset.Y >= (scrollViewer.Extent.Height - scrollViewer.Viewport.Height - threshold);

            if (!isAtBottom)
            {
                // 繝ｦ繝ｼ繧ｶ繝ｼ縺梧焔蜍輔〒荳翫↓荳翫￡縺・
                autoScroll = false;
            }
            else
            {
                // 繝ｦ繝ｼ繧ｶ繝ｼ縺瑚・蜉帙〒荳逡ｪ荳九∪縺ｧ謌ｻ縺励◆
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
                command = "縺ゅ↑縺溘・繝ｫ繝ｼ繝励↓髯･縺｣縺ｦ縺・∪縺吶ょ挨縺ｮ隕也せ縺ｧ閠・∴縺ｦ縺上□縺輔＞";
                hashHistory.Add(getHash(command));
                result = await complete(functioncallCommand, tools, cancellationToken, true);
            }
        }

    }

    // 繝ｫ繝ｼ繝励→縺ｿ縺ｪ縺呎怙蟆上・蜻ｨ譛滂ｼ井ｾ具ｼ・縺ｪ繧・A->B->A->B 繧呈､懷・・・
    private const int MinPatternLength = 2;
    // 繝ｫ繝ｼ繝励→縺ｿ縺ｪ縺咏ｹｰ繧願ｿ斐＠縺ｮ蝗樊焚・井ｾ具ｼ・縺ｪ繧・蜷後§繝代ち繝ｼ繝ｳ縺・蝗樒ｶ壹＞縺溘ｉ繝ｫ繝ｼ繝暦ｼ・
    private const int MaxRepetitions = 2;
    private bool isLoopDetected()
    {
        if (hashHistory.Count < (MinPatternLength * MaxRepetitions))
            return false;

        // 2. 繝代ち繝ｼ繝ｳ髟ｷ繧貞､牙喧縺輔○縺ｦ讀懆ｨｼ (1縺､謌ｻ繧九・縺､謌ｻ繧・..)
        // 逶ｴ霑代・n莉ｶ縺ｮ繧ｷ繝ｼ繧ｱ繝ｳ繧ｹ縺後√◎縺ｮ蜑阪・n莉ｶ縺ｨ荳閾ｴ縺吶ｋ縺九ｒ繝√ぉ繝・け縺励∪縺・
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
                // 繝ｫ繝ｼ繝玲､懷・・・
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
                    
                    // 1繝医・繧ｯ繝ｳ縺斐→縺ｫ繝ｫ繝ｼ繝励ｒ繝√ぉ繝・け
                    if (IsStreamingLoopDetected(resultItem.Text))
                    {
                        await resultItem.AppendText("\n[Loop detected: Generation stopped.]");
                        break; // foreach繧呈栢縺代※逕滓・繧剃ｸｭ譁ｭ
                    }

                    if (autoScroll)
                    {
                        // 謠冗判繧ｹ繝ｬ繝・ラ縺ｧ蜊ｳ蠎ｧ縺ｫ繝ｬ繧､繧｢繧ｦ繝医ｒ遒ｺ螳壹＆縺帙ｋ
                        Dispatcher.UIThread.Post(() =>
                        {
                            ListBox0.UpdateLayout();
                            scrollViewer?.ScrollToEnd();
                        }, DispatcherPriority.Render); // Render蜆ｪ蜈亥ｺｦ縺ｧ蜊ｳ蠎ｧ縺ｫ蜿肴丐
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
        const int minPatternLength = 5; // 遏ｭ縺吶℃繧倶ｸ閾ｴ・・ABC"縺ｪ縺ｩ・峨・辟｡隕・
        const int repeatCount = 20;      // 3蝗樣｣邯壹〒繝ｫ繝ｼ繝励→蛻､螳・

        if (string.IsNullOrEmpty(fullText) || fullText.Length < minPatternLength * repeatCount)
            return false;

        // 蠕後ｍ縺九ｉ繝代ち繝ｼ繝ｳ繧ｵ繧､繧ｺ繧貞ｺ・￡縺ｪ縺後ｉ繝√ぉ繝・け
        for (int patternSize = minPatternLength; patternSize <= fullText.Length / repeatCount; patternSize++)
        {
            string pattern = fullText.Substring(fullText.Length - patternSize);

            // --- 謾ｹ濶ｯ繝昴う繝ｳ繝・ 險伜捷繧・ｩｺ逋ｽ縺ｮ縺ｿ縺ｮ繝代ち繝ｼ繝ｳ縺ｯ繝ｫ繝ｼ繝怜愛螳壹°繧蛾勁螟・---
            // 譁・ｭ怜・縺後瑚ｨ伜捷縲∵焚蟄励∫ｩｺ逋ｽ縲阪□縺代〒讒区・縺輔ｌ縺ｦ縺・ｋ蝣ｴ蜷医・繧ｹ繧ｭ繝・・
            // (萓・ "    ", "----", "////", "1212")
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

