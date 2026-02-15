# Poss.Win.Automation.Input

Keyboard and mouse **input simulation** for Windows: key presses, clicks, cursor movement, Unicode text, window activation, and related utilities. Part of the **Poss.Win.Automation** family. Targets .NET Standard 2.0.

Use this package when you only need input simulation (no global hotkeys). If you need both input and hotkeys, use the umbrella package **Poss.Win.Automation**.

## Installation

```bash
dotnet add package Poss.Win.Automation.Input
```

## Quick Start (no filter)

Input is sent to whichever window is currently focused.

```csharp
using Poss.Win.Automation.Input;

var sim = new InputSimulator();
sim.SendText("Hello, World!");
sim.Click(100, 200, "LButton");
sim.Send("Ctrl down");
sim.Send("A");
sim.Send("Ctrl up");
```

## Window / process binding

You can bind the simulator to one or more **process** names or **window title** substrings. When bound, **all input is simulated only when the foreground window matches any of the filters**; otherwise calls like `SendText`, `Click`, `Send` are no-ops. Use this to avoid sending keystrokes to the wrong app.

### Multiple filters (params)

Pass several process names or title substrings; input is allowed when the foreground matches **any** of them. Use the **Filters** property to read the current filter collection.

```csharp
// Allow input when Notepad, Chrome, or a window titled "Visual Studio" is focused
var sim = new InputSimulator("notepad", "chrome", "Visual Studio");
sim.SendText("Sent to any of the above");

// Inspect the parsed filters
foreach (var f in sim.Filters)
    Console.WriteLine($"{f.Type}: {f.Name}");
```

### Bind by process name

Use the process name with or without `.exe`. Matching is **case-sensitive** (to avoid accidental matches with similarly named processes).

```csharp
// Notepad — process name only
var sim = new InputSimulator("notepad");
sim.SendText("Only typed when Notepad is focused");

// With .exe (same: .exe is stripped)
var sim2 = new InputSimulator("notepad.exe");

// Explicit "exe" prefix (useful if the name could be a title)
var sim3 = new InputSimulator("exe mygame");
```

### Bind by window title

Any string that does not look like a process name (no `exe ` prefix, no `.exe`) is treated as a **window title substring**. The foreground window’s title is matched **case-sensitively** (to reduce accidental matches, e.g. with browser tabs that share similar text).

```csharp
// Any window whose title contains "Visual Studio"
var sim = new InputSimulator("Visual Studio");
sim.Send("Ctrl+S");  // Only when such a window is focused

// Substring of the title bar
var sim2 = new InputSimulator("Untitled - Notepad");
```

### Check and activate

- **IsActiveWindow()** — `true` if the current foreground window matches **any** of the filters passed to the constructor.
- **IsActiveWindow(string query)** — `true` if the current foreground matches the given process/title query.
- **WinActivate(string query)** — finds a window matching the query and brings it to the foreground; returns `true` if a window was activated.

```csharp
var sim = new InputSimulator("notepad");

// Bring Notepad to front, then type only into it
if (sim.WinActivate("notepad"))
{
    sim.SendText("Typed into Notepad");
}

// Optional: check before sending
if (sim.IsActiveWindow())
    sim.Click(100, 100);
```

### Full example: process-bound automation

```csharp
using Poss.Win.Automation.Input;

// Only send input when "MyEditor.exe" is the active window
var sim = new InputSimulator("MyEditor");

sim.WinActivate("MyEditor");  // Focus the app first
sim.SendText("Hello");
sim.Send("Ctrl+S");
sim.MouseWheel(120);          // Scroll up — no-op if another app is focused

// Without a filter, input goes to whatever is focused
var simAny = new InputSimulator();
simAny.Send("Escape");
```

## Main types

- **InputSimulator** — `SendText`, `Send`, `Click`, `MouseWheel`, `MouseSetPos`, `MouseDeltaMove`, `MouseGetPos`, `WinActivate`, `GetWindowTitle`, `GetWindowPos`, `GetWindowProcessName`, `IsActiveWindow`, `GetKeyState`, **Filters** (read-only filter collection), etc.
- **WindowFilter** (struct) — `Name`, `Type` (process or window title). Used in the **Filters** collection.
- **WindowFilterKind** (enum) — `Process`, `Window`.
- **ForegroundIdentity** (struct) — `Hwnd`, `Pid`; identifies a window for caching (e.g. to avoid stale match results when a process exits and the handle is reused).

## Requirements

- .NET Standard 2.0
- Windows (uses Win32 `SendInput`, etc.)

## License

MIT. See [LICENSE](https://github.com/latur-h/Poss.Win.Automation/blob/main/LICENSE) in the repository.

## Repository

[https://github.com/latur-h/Poss.Win.Automation](https://github.com/latur-h/Poss.Win.Automation)
