# Poss.Win.Automation

<em>Windows automation library</em> for keyboard and mouse input simulation, global hotkey registration, and window management. Targets .NET Standard 2.0.

## Features

- **InputSimulator** — simulate keyboard and mouse input (key presses, clicks, cursor movement, Unicode text)
- **GlobalHotKeyManager** — register system-wide hotkeys with keyboard and mouse support
- **Window utilities** — activate windows, query titles, positions, process names

## Installation

```bash
dotnet add package Poss.Win.Automation
```

## Quick Start

### Input simulation

```csharp
using Poss.Win.Automation.Input;

var sim = new InputSimulator();
sim.SendText("Hello, World!");
sim.Click(100, 200, "LButton");
sim.Send("Ctrl");
```

### Global hotkeys

```csharp
using Poss.Win.Automation.HotKeys;

var manager = new GlobalHotKeyManager(new HotKeyManagerOptions { RunMessageLoop = true });
manager.Start();

manager.Register("save", async () => { /* save action */ }, "Ctrl+S");
manager.Register("custom", async () => { /* action */ }, "Shift+LButton Up");

// manager.Stop(); manager.Dispose();
```

### Process filtering (input only when window is active)

```csharp
using Poss.Win.Automation.Input;

// Restrict input to Notepad — SendText/Click/etc. are no-ops when another app is focused
var sim = new InputSimulator("notepad");
sim.SendText("Typed into Notepad only");
sim.Click(100, 100);
```

### Console apps: use RunMessageLoop

```csharp
using Poss.Win.Automation.HotKeys;

// Hooks require a Windows message loop. In WinForms/WPF it exists; in console apps you must enable it:
var manager = new GlobalHotKeyManager(new HotKeyManagerOptions { RunMessageLoop = true });
manager.Start();
manager.Register("quit", async () => { Environment.Exit(0); }, "Ctrl+Q");
// Keep running
```

### Key actions: Down, Up, Press

```csharp
sim.Send("Ctrl down");   // Hold Ctrl
sim.Send("A");          // Press A (with Ctrl held)
sim.Send("Ctrl up");    // Release Ctrl

// Trigger-on-release hotkey
manager.Register("onRelease", async () => { /* fires when key is released */ }, "F5 Up");
```

### Mouse and window utilities

```csharp
sim.MouseSetPos(500, 300);
sim.MouseDeltaMove(600, 400, speed: 2.0);  // Smooth movement with easing
sim.MouseWheel(120);   // Scroll up (negative = down)

sim.WinActivate("notepad");  // Activate first window matching process or title
var pos = sim.MouseGetPos();
var title = sim.GetWindowTitle();
```

## Limitations

- **Windows only** — Uses Win32 APIs (SendInput, SetWindowsHookEx, etc.). Does not run on Linux or macOS.
- **UIPI** — Input sent via SendInput may be blocked when targeting elevated processes from a non-elevated app (and vice versa).
- **Message loop** — Global hotkeys need a Windows message loop. In console apps, set `RunMessageLoop = true`.
- **Process filter** — InputSimulator with a process filter only simulates input when the matching window is foreground; calls are no-ops otherwise.

## Keywords

`windows`, `automation`, `input`, `keyboard`, `mouse`, `hotkey`, `global hotkey`, `keyboard hook`, `mouse hook`, `input simulation`, `SendInput`, `Win32`, `.NET Standard`, `C#`

## Documentation

- [Public API Reference](Documents/PublicAPI.md) — full list of public types and members

## Requirements

- .NET Standard 2.0
- Windows (uses Win32 APIs: SendInput, SetWindowsHookEx, etc.)
