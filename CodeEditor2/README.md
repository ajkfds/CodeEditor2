# CodeEditor2 Parse Architecture Analysis

## Overview
Core editor component responsible for text editing, code parsing, and UI synchronization in RtlEditor2.

## Key Components

### Parser System

#### ParseWorker (`CodeEditor/Parser/ParseWorker.cs`)
Handles asynchronous background parsing of text files.

```csharp
public async Task Parse(Data.TextFile textFile)
{
    // Cancels previous task if running
    if (_cts != null) _cts.Cancel();
    _cts = new CancellationTokenSource();
    _currentTask = Task.Run(async () => { await runParse(textFile, token); }, token);
}
```

**Issue:** Rapid file switching causes task cancellation + immediate restart, leading to potential race conditions.

#### CodeViewParser (`CodeEditor/Parser/CodeViewParser.cs`)
Entry point for parse requests from the UI.

```csharp
public void EntryParse()
{
    if (codeView.TextFile == null) return;
    ParseWorker worker = new ParseWorker();
    Task.Run(async () => { await worker.Parse(textFile); }); // Fire and forget
}
```

**Issue:** Parse completion may update stale file if user switched files during parse.

### TextFile Base Class (`Data/TextFile.cs`)

Thread-safe text file implementation using `ReaderWriterLockSlim`:

```csharp
protected readonly ReaderWriterLockSlim textFileLock = 
    new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
```

**Protected Members:**
- `_parsedDocument` - Current parsed document
- `_document` - CodeDocument reference
- `_reparseRequested` - Parse request flag
- `_storedVerticalScrollPosition` - UI state

**Race Condition Points:**

1. **Version Check in AcceptParsedDocumentAsync:**
```csharp
if (codeDoc.Version != vParsedDocument.Version)
{
    vParsedDocument.ReparseRequested = true;
    return; // Stale result rejected
}
```
The version check helps but doesn't prevent all races.

2. **Items Access:**
```csharp
lock (item.Items) { /* clear and update */ }
```
Uses lock but the pattern (clear before add) creates inconsistency windows.

### CodeDocument (`CodeEditor/CodeDocument.cs`)

Manages document state, color information, and marks.

**Key Properties:**
```csharp
public ulong Version { get; set; } = 0;
public ulong CleanVersion { get; private set; } = 0;
public MarkHandler Marks;
public ColorHandler TextColors;
public HIghLightHandler HighLights;
public FoldingHandler Foldings;
```

**Color Information Flow:**
```
Parser writes colors -> CodeDocument.TextColors
                    -> ColorHandler.LineInformation
UI reads colors     <- TextDocument.LineTransformers
                   <- CodeDocumentColorTransformer
```

**Issue:** Color information is not version-stamped. When `CopyColorMarkFrom` is called:
```csharp
public void CopyColorMarkFrom(CodeDocument document)
{
    TextColors.LineInformation = document.TextColors.LineInformation;
    lock (Marks.marks)
    {
        Marks.marks = new List<CodeDrawStyle.MarkDetail>(document.Marks.marks);
    }
    // ...
}
```

If the source document is modified during copy, colors may be inconsistent.

### MarkHandler (`CodeEditor/CodeDocument/MarkHandler.cs`)

Manages error/warning markers on the document.

```csharp
public void SetMarkAt(int index, int length, byte value)
{
    // Adds mark to marks list
    lock (marks)
    {
        if (mark.LastOffset > mark.Offset) marks.Add(mark);
    }
}

public void OnTextEdit(DocumentChangeEventArgs e)
{
    // Adjusts mark positions after text edit
    for (int i = 0; i < marks.Count; i++)
    {
        // Logic to shift marks based on edit position
    }
}
```

**Issue:** Marks can be modified by both parser (SetMarkAt) and UI thread (OnTextEdit) simultaneously.

### ColorHandler (`CodeEditor/CodeDocument/ColorHandler.cs`)

Manages syntax highlighting colors per line.

```csharp
public Dictionary<int, LineInformation> LineInformation = new Dictionary<int, LineInformation>();

public virtual void SetColorAt(int index, byte value, int length)
{
    // Sets color for a range of text
    LineInformation lineInfo = GetLineInformation(lineStart.LineNumber);
    lock (lineInfo.Colors)
    {
        lineInfo.Colors.Add(new LineInformation.Color(offset, length, color));
    }
}
```

**Issue:** Multi-line color ranges create entries in multiple lines. If parsing is interrupted, partial color information may remain.

### Controller CodeEditor (`Controller_CodeEditor.cs`)

Manages UI-editor interaction.

```csharp
public static void PostRefresh()
{
    Global.codeView.Redraw();
    Global.codeView.UpdateMarks();
}

public static void EntryParse()
{
    Dispatcher.UIThread.Invoke(Global.codeView.codeViewParser.EntryParse);
}
```

**Issue:** `EntryParse()` fires-and-forgets the parse task. There's no coordination with completion.

### CodeView (`Views/CodeView.axaml.cs`)

Main editor view component.

```csharp
public void UpdateMarks()
{
    _markerRenderer.ClearMark();
    if (CodeDocument == null) return;
    _markerRenderer.SetMarks(CodeDocument.Marks.marks);
}
```

**Issue:** Directly reads from CodeDocument.Marks without synchronization.

## Parallel Parse Architecture

```
User Click -> OnSelected() -> PostParseAsync()
                                ↓
                         ParseHierarchy.ParseAsync()
                                ↓
                    ┌─────────────────────────┐
                    │   runParallel()          │
                    │   - Multiple Workers    │
                    │   - Cancellation Token  │
                    │   - Work Queue          │
                    └─────────────────────────┘
                                ↓
                    ┌─────────────────────────┐
                    │ parseTextFile()         │
                    │ - CreateParser()        │
                    │ - parser.ParseAsync()   │
                    │ - AcceptParsedDoc()     │
                    └─────────────────────────┘
                                ↓
                    ┌─────────────────────────┐
                    │   UpdateAsync()         │
                    │   - Updater.UpdateAsync │
                    │   - UpdateVisual()      │
                    └─────────────────────────┘
```

## Identified Race Conditions

### 1. ParseWorker → ParseWorker Race
Multiple `EntryParse()` calls create new ParseWorker instances that may interleave.

### 2. CodeDocument Version Mismatch
When `CopyColorMarkFrom` copies between documents with different versions.

### 3. MarkHandler Concurrent Access
Parser adds marks while UI thread adjusts them via `OnTextEdit`.

### 4. Items Dictionary Modification
`Updater.UpdateAsync` clears Items before adding new ones, leaving empty state temporarily.

### 5. Parse Mode Sequencing
LoadParse, BackgroundParse, EditParse modes don't have proper sequencing guarantees.

### 6. Root BuildingBlocks Dictionary
Multiple parallel parsers modify the same shared dictionary.

## Investigation Status
- [x] ParseWorker architecture analysis
- [x] CodeViewParser entry point analysis
- [x] TextFile thread-safety analysis
- [x] CodeDocument shared state analysis
- [x] MarkHandler synchronization analysis
- [x] ColorHandler race condition analysis
- [x] Controller.PostRefresh analysis
- [ ] Fix implementation planning
- [ ] Test verification

## Related Files
- `CodeEditor/Parser/ParseWorker.cs`
- `CodeEditor/Parser/CodeViewParser.cs`
- `Data/TextFile.cs`
- `CodeEditor/CodeDocument.cs`
- `CodeEditor/CodeDocument/MarkHandler.cs`
- `CodeEditor/CodeDocument/ColorHandler.cs`
- `Controller_CodeEditor.cs`
- `Views/CodeView.axaml.cs`
