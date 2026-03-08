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

/// <summary>
/// LLM Chat Control
/// UI control providing interactive interface with AI agent
/// Features include streaming responses, tool calls, and loop detection
/// </summary>
public partial class ChatControl : UserControl
{
    /// <summary>
    /// Constructor
    /// Initializes components and binds events
    /// </summary>
    public ChatControl()
    {
        DataContext = this;
        InitializeComponent();

        // Skip initialization in design mode
        if (Design.IsDesignMode)
        {
            return;
        }
        // Add input item to chat list
        Items.Add(inputItem);

        // Key binding to focus send button on Enter key press
        var keyBinding = new KeyBinding
        {
            Gesture = new KeyGesture(Key.Enter, KeyModifiers.None),
            Command = ReactiveCommand.Create(() =>
            {
                inputItem.SendButton.Focus();
            }),
        };
        inputItem.TextBox.KeyBindings.Add(keyBinding);

        // Register click event handlers for various buttons
        inputItem.SendButton.Click += SendButton_Click;
        inputItem.SaveButton.Click += SaveButton_Click;
        inputItem.ClearButton.Click += ClearButton_Click;
        inputItem.LoadButton.Click += LoadButton_Click;
        inputItem.AbortButton.Click += AbortButton_Click;

        // Focus textbox on initialization
        inputItem.TextBox.Focus();

        // Register for ListBox Loaded event
        ListBox0.Loaded += ListBox0_Loaded;
    }


    // ScrollViewer and auto-scroll control fields
    private ScrollViewer scrollViewer;
    /// <summary>
    /// Flag indicating whether system is scrolling
    /// Used for auto-scroll control
    /// </summary>
    private bool _isInternalScrolling = false;
    /// <summary>
    /// Initialization complete flag
    /// Prevents duplicate execution of ResetAsync
    /// </summary>
    private bool initialized = false;

    /// <summary>
    /// Handles ListBox loaded event
    /// Initializes scrollviewer settings and auto-scroll functionality
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private void ListBox0_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Find ScrollViewer
        var scrollViewer = ListBox0.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null) throw new Exception();

        scrollViewer = ListBox0.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null) return;

        // Handle scroll extent change (for auto-scroll)
        scrollViewer.GetObservable(ScrollViewer.ExtentProperty).Subscribe(_ =>
        {
            if (autoScroll)
            {
                _isInternalScrolling = true; // Signal that system is about to scroll
                // Scroll to bottom
                scrollViewer.Offset = new Vector(scrollViewer.Offset.X, double.MaxValue);

                // Reset flag when render cycle completes
                Dispatcher.UIThread.Post(() => _isInternalScrolling = false, DispatcherPriority.Loaded);
            }
        });

        // Handle scroll position change
        scrollViewer.ScrollChanged += (s, ev) =>
        {
            // Skip manual check if scrolling is caused by system (extent change)
            if (_isInternalScrolling) return;

            // Threshold (in pixels) for determining if near bottom
            const double threshold = 30; // Add some buffer
            bool isAtBottom = scrollViewer.Offset.Y >= (scrollViewer.Extent.Height - scrollViewer.Viewport.Height - threshold);

            if (!isAtBottom)
            {
                // Disable auto-scroll when user manually scrolls up
                autoScroll = false;
            }
            else
            {
                // Enable auto-scroll when user returns to bottom
                autoScroll = true;
            }
        };
        this.scrollViewer = scrollViewer;

        // Execute reset only on first load
        if (!initialized)
        {
            initialized = true;
            Dispatcher.UIThread.Post(async () =>
            {
                await ResetAsync();
            });
        }
    }

    /// <summary>
    /// Collection of chat items
    /// List of chat messages displayed in UI
    /// </summary>
    public ObservableCollection<ChatItem> Items { get; set; } = new ObservableCollection<ChatItem>();

    /// <summary>
    /// Reference to LLM chat frontend
    /// Interface for communication with AI
    /// </summary>
    private ILLMChatFrontEnd? chat { get; set; } = null;

    /// <summary>
    /// Reference to LLM agent
    /// Manages tool definitions and prompts
    /// </summary>
    private LLMAgent? agent { get; set; } = null;

    /// <summary>
    /// Auto-scroll enabled flag
    /// Whether to auto-scroll when new message is added
    /// </summary>
    private bool autoScroll { get; set; } = true;

    /// <summary>
    /// Auto-save flag
    /// Whether to save when new message is added
    /// </summary>
    public bool AutoSave { set; get; } = false;

    /// <summary>
    /// Log file path
    /// Storage location for chat history
    /// </summary>
    public string? LogFilePath { set; get; } = null;

    /// <summary>
    /// Input item (textbox and buttons)
    /// </summary>
    InputItem inputItem = new InputItem();

    /// <summary>
    /// Reference to last result item
    /// Used for tracking streaming response
    /// </summary>
    CollapsibleTextItem? lastResultItem = null;

    /// <summary>
    /// Display color for command (user input)
    /// Light blue
    /// </summary>
    private Avalonia.Media.Color commandColor = Avalonia.Media.Colors.CadetBlue;// new Avalonia.Media.Color(255, 255, 200, 200);

    /// <summary>
    /// Display color for completion (AI response)
    /// </summary>
    private Avalonia.Media.Color completeColor = Avalonia.Media.Colors.YellowGreen;// new Avalonia.Media.Color(255, 200, 255, 255);

    /// <summary>
    /// Input acceptable flag
    /// Acts as reentrant lock
    /// </summary>
    bool inputAcceptable = true;

    /// <summary>
    /// Cancellation token source
    /// Used to cancel ongoing async operations
    /// </summary>
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();


    // External control methods

    /// <summary>
    /// Set model and agent
    /// Must be called before chat control becomes usable
    /// </summary>
    /// <param name="chatModel">LLM chat frontend implementation</param>
    /// <param name="agent">LLM agent (optional)</param>
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

    /// <summary>
    /// Reset chat
    /// Clears messages and restarts from agent's base prompt
    /// </summary>
    /// <returns>Async operation task</returns>
    public async Task ResetAsync()
    {
        if (chat == null) return;

        // Reset chat
        await chat.ResetAsync();

        // Clear items (except input item)
        int itemCount = Items.Count;
        for (int i = 0; i < itemCount - 2; i++)
        {
            Items.RemoveAt(1);
        }

        // Create new cancellation token
        cancellationTokenSource = new CancellationTokenSource();

        // Start from base prompt if agent is set
        if (agent != null)
        {
            string basePrompt = await agent.GetBasePromptAsync(cancellationTokenSource.Token);
            if (basePrompt == "") return;
            await UserComplete(basePrompt, CollapsibleTextItem.MessageType.functionCallReturn);
        }
    }

    /// <summary>
    /// Process user input and send to LLM
    /// </summary>
    /// <param name="command">User input text</param>
    /// <param name="collapse">Whether to collapse input</param>
    /// <returns>Async operation task</returns>
    private async Task UserComplete(string command, CollapsibleTextItem.MessageType messageType = CollapsibleTextItem.MessageType.command)
    {
        // Ignore if input is not acceptable
        if (!inputAcceptable) return;

        // Create new cancellation token
        cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        // Clear hash history
        hashHistory.Clear();

        // Get tools
        IList<AITool>? tools = null;
        if (agent != null)
        {
            tools = agent.Tools;
            if (tools.Count == 0) tools = null;
        }
        // Execute command with tool calls
        await completeWithFunctionCall(command, tools, cancellationToken, messageType);
    }

    /// <summary>
    /// Calculate hash value of text
    /// Used for loop detection
    /// </summary>
    /// <param name="text">Text to hash</param>
    /// <returns>Hex string of hash value</returns>
    private string getHash(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        byte[] hashBytes = XxHash64.Hash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Hash history of command/response
    /// Used for loop detection
    /// </summary>
    private List<string> hashHistory = new List<string>();

    /// <summary>
    /// Execute interaction with LLM, calling tools as needed
    /// Attempts retry from different perspective if agent is in a loop
    /// </summary>
    /// <param name="command">Command to send</param>
    /// <param name="tools">List of available AI tools</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="collapseQuestion">Whether to collapse question item</param>
    /// <returns>Async operation task</returns>
    private async Task completeWithFunctionCall(
        string command, IList<AITool>? tools,
        CancellationToken cancellationToken,
        CollapsibleTextItem.MessageType messageType
        )
    {
        string? result;
        bool loopAlarted = false;
        string loopedHash = "";

        // Add hash of command
        hashHistory.Add(getHash(command));
        result = await complete(command, tools, cancellationToken, messageType);
        if (result == null) return;
        hashHistory.Add(getHash(result));

        // Loop while agent includes function calls in response
        while (agent != null && !cancellationToken.IsCancellationRequested)
        {
            // Parse function call from response
            string? functioncallCommand = await agent.ParseResponceAsync(result, cancellationToken);
            if (functioncallCommand == null) return;

            hashHistory.Add(getHash(functioncallCommand));
            result = await complete(functioncallCommand, tools, cancellationToken, CollapsibleTextItem.MessageType.functionCallReturn);
            if (result == null) return;
            hashHistory.Add(getHash(result));

            // Loop detection
            if (isLoopDetected())
            {
                // Exit if same loop is detected again
                if (loopAlarted && hashHistory.Last() == loopedHash)
                {
                    return;
                }
                loopAlarted = true;
                loopedHash = hashHistory.Last();
                // Prompt to try escaping from loop
                command = "You are in a loop. Please think from a different perspective.";
                hashHistory.Add(getHash(command));
                result = await complete(functioncallCommand, tools, cancellationToken, CollapsibleTextItem.MessageType.command);
            }
        }
    }

    // Loop detection constants
    /// <summary>
    /// Minimum pattern length to consider as loop (e.g., 2 detects A-&gt;B-&gt;A-&gt;B)
    /// </summary>
    private const int MinPatternLength = 2;
    /// <summary>
    /// Number of repetitions to consider as loop (e.g., 3 means loop if same pattern repeats 3 times)
    /// </summary>
    private const int MaxRepetitions = 3;

    /// <summary>
    /// Detect loop from hash history
    /// Determines if same pattern is repeating
    /// </summary>
    /// <returns>True if loop is detected</returns>
    private bool isLoopDetected()
    {
        if (hashHistory.Count < (MinPatternLength * MaxRepetitions))
            return false;

        // Verify by varying pattern length (1 back, 2 back...)
        // Check if the last n items match the previous n items
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
                // Loop detected!
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Execute interaction with LLM and process streaming response
    /// Includes progress timer, loop detection, and auto-scroll functionality
    /// </summary>
    /// <param name="command">Command to send</param>
    /// <param name="tools">List of available AI tools</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="collapseQuestion">Whether to collapse question item</param>
    /// <returns>Response text, or null</returns>
    private async Task<string?> complete(
        string command, IList<AITool>? tools,
        CancellationToken cancellationToken,
        CollapsibleTextItem.MessageType messageType
        )
    {
        if (chat == null) return null;

        // Reentrant lock: make input unacceptable
        inputAcceptable = false;

        try
        {
            // Create and display command item
            CollapsibleTextItem commandItem = new CollapsibleTextItem(command,messageType);
            inputItem.TextBox.Text = "";
            commandItem.TextColor = commandColor;
            Items.Insert(Items.Count - 1, commandItem);
            if (messageType == CollapsibleTextItem.MessageType.functionCallReturn) commandItem.Collapsed = true;

            // Create result item
            CollapsibleTextItem resultItem = new CollapsibleTextItem("",CollapsibleTextItem.MessageType.responce);
            Items.Insert(Items.Count - 1, resultItem);
            lastResultItem = resultItem;

            // Start progress timer
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

            // Execute chat command
            try
            {
                // Process streaming response
                await foreach (string ret in chat.GetAsyncCollectionChatResult(command, tools, cancellationToken))
                {
                    // Stop timer on first result received
                    if (timerActivate & ret != "")
                    {
                        timerCancellationTokenSource.Cancel();
                        timerActivate = false;
                        await resultItem.SetText("");
                    }
                    // Append result text
                    await resultItem.AppendText(ret);

                    // Check for loop on each token
                    if (IsStreamingLoopDetected(resultItem.Text))
                    {
                        await resultItem.AppendText("\n[Loop detected: Generation stopped.]");
                        break; // Exit foreach to stop generation
                    }

                    // If auto-scroll is enabled
                    if (autoScroll)
                    {
                        // Set flag that system is scrolling
                        _isInternalScrolling = true;
                        // Immediately finalize layout on render thread
                        Dispatcher.UIThread.Post(() =>
                        {
                            ListBox0.UpdateLayout();
                            scrollViewer?.ScrollToEnd();
                            // Reset flag after scroll completes
                            _isInternalScrolling = false;
                        }, DispatcherPriority.Render); // Immediate reflection at Render priority
                    }
                }
                if (timerActivate)
                {
                    timerCancellationTokenSource.Cancel();
                    timerActivate = false;
                    await resultItem.SetText("blank");
                }
            }
            // Do nothing if cancelled
            catch (OperationCanceledException ex)
            {

            }
            catch (Exception ex)
            {
                CodeEditor2.Controller.AppendLog(ex.Message, Avalonia.Media.Colors.Red);
            }
            // Wait for timer task to complete
            await displayTimerTask;
            stopwatch.Stop();

            // Change color to completion color and save messages
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
            // Resume input acceptance
            inputAcceptable = true;
        }
        return null;
    }

    /// <summary>
    /// Detect loop during streaming response
    /// Patterns with only symbols or whitespace are excluded from loop detection
    /// </summary>
    /// <param name="fullText">Full text received so far</param>
    /// <returns>True if loop is detected</returns>
    private bool IsStreamingLoopDetected(string fullText)
    {
        const int minPatternLength = 5; // Ignore too short matches like "ABC"
        const int repeatCount = 20;      // Detect as loop if repeating 3 times consecutively

        if (string.IsNullOrEmpty(fullText) || fullText.Length < minPatternLength * repeatCount)
            return false;

        // Check from back while expanding pattern size
        for (int patternSize = minPatternLength; patternSize <= fullText.Length / repeatCount; patternSize++)
        {
            string pattern = fullText.Substring(fullText.Length - patternSize);

            // Exclude patterns with only symbols or whitespace from loop detection
            // Skip if string consists only of "symbols, digits, whitespace"
            // (e.g., "    ", "----", "////", "1212")
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

    /// <summary>
    /// Get chat results as async collection
    /// Uses IAsyncEnumerable to support streaming responses
    /// </summary>
    /// <param name="command">Command to send</param>
    /// <param name="tools">List of available AI tools</param>
    /// <param name="cancellation">Cancellation token</param>
    /// <returns>Async enumerable of response text</returns>
    public async IAsyncEnumerable<string> GetAsyncCollectionChatResult(string command, IList<AITool>? tools, [EnumeratorCancellation] CancellationToken cancellation)
    {
        inputItem.TextBox.Text = command;
        await complete(command, tools, cancellation,CollapsibleTextItem.MessageType.command);
        if (lastResultItem == null) yield break;
        yield return lastResultItem.Text;
    }

    /// <summary>
    /// Get chat result asynchronously
    /// Aggregates streaming results into a single string
    /// </summary>
    /// <param name="command">Command to send</param>
    /// <param name="tools">List of available AI tools</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response text</returns>
    public async Task<string> GetAsyncChatResult(string command, IList<AITool>? tools, CancellationToken cancellationToken)
    {
        StringBuilder sb = new StringBuilder();
        await foreach (string ret in GetAsyncCollectionChatResult(command, tools, cancellationToken))
        {
            sb.Append(ret);
        }
        return sb.ToString();
    }

    // Save and Load ////////////////////

    /// <summary>
    /// Load messages from specified path
    /// </summary>
    /// <param name="filePath">Message file path</param>
    /// <returns>Async operation task</returns>
    public async Task LoadMessagesAsync(string filePath)
    {
        LogFilePath = filePath;
        await LoadMessagesAsync();
    }

    /// <summary>
    /// Load messages from current LogFilePath
    /// </summary>
    /// <returns>Async operation task</returns>
    public async Task LoadMessagesAsync()
    {
        if (chat == null) return;
        if (LogFilePath == null) return;

        // Load messages from chat backend
        await chat.LoadMessagesAsync(LogFilePath);

        // Remove existing messages from UI
        int items = Items.Count;
        for (int i = 0; i < items - 2; i++)
        {
            Items.RemoveAt(1);
        }

        // Get and display chat messages
        List<Microsoft.Extensions.AI.ChatMessage> chatmessages = chat.GetChatMessages();
        foreach (Microsoft.Extensions.AI.ChatMessage chatmessage in chatmessages)
        {
            CollapsibleTextItem resultItem = new CollapsibleTextItem(chatmessage.Text,CollapsibleTextItem.MessageType.responce);
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

    /// <summary>
    /// Save messages to specified path
    /// </summary>
    /// <param name="filePath">Destination file path</param>
    /// <returns>Async operation task</returns>
    public async Task SaveMessagesAsync(string filePath)
    {
        LogFilePath = filePath;
        await SaveMessagesAsync();
    }

    /// <summary>
    /// Save messages to current LogFilePath
    /// </summary>
    /// <returns>Async operation task</returns>
    private async Task SaveMessagesAsync()
    {
        if (chat == null) return;
        if (LogFilePath == null) return;
        await chat.SaveMessagesAsync(LogFilePath);
    }


    // UI Control Event Handlers

    /// <summary>
    /// Handles abort button click
    /// Cancels ongoing async operation
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private void AbortButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Handles load button click
    /// Load chat history from file
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
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

    /// <summary>
    /// Handles clear button click
    /// Reset chat
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
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

    /// <summary>
    /// Handles save button click
    /// Save chat history to file
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
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

    /// <summary>
    /// Handles send button click
    /// Send user input to LLM
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private async void SendButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            string? command = inputItem.TextBox.Text;
            if (command == null) return;

            // Process user input
            await UserComplete(command);

            // Focus textbox to maintain BM
            inputItem.TextBox.Focus();
        }
        catch (Exception exception)
        {
            CodeEditor2.Controller.AppendLog(exception.Message, Avalonia.Media.Colors.Red);
        }
    }

}
