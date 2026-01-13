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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CodeEditor2.LLM.ChatControl;
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
    }

    public ObservableCollection<ChatItem> Items { get; set; } = new ObservableCollection<ChatItem>();

    private ILLMChatFrontEnd? chat { get; set; } = null;
    private LLMAgent? agent { get; set; } = null;
    private bool autoScroll { get; set; } = true;

    public bool AutoSave { set; get; } = false;
    public string? LogFilePath { set; get; } = null;

    InputItem inputItem = new InputItem();
    TextItem? lastResultItem = null;

    private Avalonia.Media.Color commandColor = new Avalonia.Media.Color(255, 255, 200, 200);
    private Avalonia.Media.Color completeColor = new Avalonia.Media.Color(255, 200, 255, 255);

    bool inputAcceptable = true;

    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();


    // external control
    public async Task SetModelAsync(ILLMChatFrontEnd chatModel, LLMAgent? agent)
    {
        this.chat = chatModel;
        this.agent = agent;
        //        Model = model;
        //        chat = new OpenRouterChat(Model, enableFunctionCalling);
        //        Items.Add(new TextItem(Model.Caption + "\n"));
        await ResetAsync();
    }

    public async Task ResetAsync()
    {
        if (chat == null) return;
        await chat.ResetAsync();
        int itemCount = Items.Count;
        for (int i = 0; i < itemCount - 2; i++)
        {
            Items.RemoveAt(1);
        }

        if (agent != null)
        {
            string basePrompt = await agent.GetBasePromptAsync(cancellationTokenSource.Token);
            if (basePrompt == "") return;
            IList<AITool>? tools = agent.Tools;
            if (tools.Count == 0) tools = null;
            await Complete(basePrompt, tools, cancellationTokenSource.Token);
        }
    }
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
            TextItem resultItem = new TextItem(chatmessage.Text);
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

    private async Task UserComplete(string command, CancellationToken cancellation)
    {
        if (!inputAcceptable) return;

        IList<AITool>? tools = null;
        if (agent != null)
        {
            string basePrompt = await agent.GetBasePromptAsync(cancellationTokenSource.Token);
            tools = agent.Tools;
            if (tools.Count == 0) tools = null;
        }
        await Complete(command, tools, cancellationTokenSource.Token);
    }

    private async Task Complete(string command, IList<AITool>? tools, CancellationToken cancellation)
    {
        if (chat == null) return;
        // reentrant lock
        inputAcceptable = false;

        try
        {
            TextItem commandItem = new TextItem(command);
            inputItem.TextBox.Text = "";
            commandItem.TextColor = commandColor;
            Items.Insert(Items.Count - 1, commandItem);

            TextItem resultItem = new TextItem("");
            Items.Insert(Items.Count - 1, resultItem);
            lastResultItem = resultItem;

            // show progress timer
            var stopwatch = Stopwatch.StartNew();

            bool timerActivate = true; // timer activate flag, timer will be stopped when first result is returned
            using var cts = new CancellationTokenSource();
            var displayTimerTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (timerActivate)
                        {
                            await resultItem.SetText("waiting ... " + (stopwatch.ElapsedMilliseconds / 1000f).ToString("F1") + "s");
                        }
                        await Task.Delay(100, cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, cts.Token);


            // execute chat command
            try
            {
                await foreach (string ret in chat.GetAsyncCollectionChatResult(command, tools, cancellation))
                {
                    if (timerActivate & ret != "")
                    {
                        cts.Cancel();
                        timerActivate = false;
                        await resultItem.SetText("");
                    }
                    await resultItem.AppendText(ret);

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
                    cts.Cancel();
                    timerActivate = false;
                    await resultItem.SetText("blank");
                }
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
            if (agent != null)
            {
                string? funcRet = await agent.ParseResponceAsync(result, cancellation);
                if (funcRet != null)
                {
                    await Complete(funcRet, tools, cancellation);
                }
            }
        }
        catch (Exception ex)
        {
            CodeEditor2.Controller.AppendLog(ex.Message, Avalonia.Media.Colors.Red);
        }
        finally
        {
            inputAcceptable = true;
            cancellationTokenSource = new CancellationTokenSource();
        }
    }

    public async IAsyncEnumerable<string> GetAsyncCollectionChatResult(string command, IList<AITool>? tools, [EnumeratorCancellation] CancellationToken cancellation)
    {
        inputItem.TextBox.Text = command;
        await Complete(command, tools, cancellation);
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

            await UserComplete(command, cancellationTokenSource.Token);

            inputItem.TextBox.Focus();
        }
        catch (Exception exception)
        {
            CodeEditor2.Controller.AppendLog(exception.Message, Avalonia.Media.Colors.Red);
        }
    }

    // etc




}

