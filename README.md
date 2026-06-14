# Poss.Win.Automation

<em>Windows automation library</em> for keyboard and mouse input simulation, global hotkey registration, and window management. Targets .NET Standard 2.0.

## Features

- **InputSimulator** — simulate keyboard and mouse input (key presses, clicks, cursor movement, Unicode text)
- **Key parsing prechecks** — validate key/chord strings with `TryParse` / `TryParseKey` before sending input or registering hotkeys
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
using Poss.Win.Automation.GlobalHotKeys;

var manager = new GlobalHotKeyManager(new GlobalHotKeyManagerOptions { RunMessageLoop = true });
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
using Poss.Win.Automation.GlobalHotKeys;

// Hooks require a Windows message loop. In WinForms/WPF it exists; in console apps you must enable it:
var manager = new GlobalHotKeyManager(new GlobalHotKeyManagerOptions { RunMessageLoop = true });
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

### Validate key strings (precheck)

Use `TryParse` / `TryParseKey` to validate key names **before** calling `Send`, `GetKeyState`, or registering hotkeys — without throwing.

```csharp
using Poss.Win.Automation.Common.Keys.Enums;
using Poss.Win.Automation.Common.Structs;
using Poss.Win.Automation.Input;
using Poss.Win.Automation.GlobalHotKeys.Structs;

// Single key name only (no action): "1", "D1", "Ctrl", "LButton", ";"
if (KeyStroke.TryParseKey("1", out VirtualKey key))
    Console.WriteLine(key);  // D1

// Raw VK code for a key name
if (KeyStroke.TryGetVirtualKeyCode("F5", out ushort vkCode)) { /* ... */ }

// Key + optional action: "A", "D1 Down", "LButton click"
if (KeyStroke.TryParse("Ctrl Down", out KeyStroke stroke))
    sim.Send(stroke);

// Same as KeyStroke.TryParse, with InputSimulator's parse cache
if (InputSimulator.TryParse("1 Down", out KeyStroke cached))
    sim.Send(cached);

// Chord (parts separated by '+'): "Ctrl + 1", "Shift + LButton Up"
if (HotkeyCombination.TryParse("Ctrl + S", out _))
    manager.Register("save", async () => { }, "Ctrl + S");
```

Accepted key formats:

- **Enum names** — `D1`, `Ctrl`, `F5`, `LButton`, `NumPad3` (case-insensitive)
- **Row digits** — `"0"`–`"9"` map to `D0`–`D9` (not mouse VK codes)
- **Single letters** — `A` / `a`
- **Symbol literals** — `;` `=` `,` `-` `.` `/` `` ` `` `[` `\` `]` `'`

Space in a stroke string separates **key from action** (`Down`, `Up`, `Press`, `Click`), not multiple keys. Use `+` for chords (`HotkeyCombination`). Multi-digit numeric strings (e.g. `"17"`) are rejected.

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
