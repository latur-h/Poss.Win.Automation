# Poss.Win.Automation.Input

Keyboard and mouse **input simulation** for Windows: key presses, clicks, cursor movement, Unicode text, window activation, and related utilities. Part of the **Poss.Win.Automation** family. Targets .NET Standard 2.0.

Use this package when you only need input simulation (no global hotkeys). If you need both input and hotkeys, use the umbrella package **Poss.Win.Automation**.

## Installation

```bash
dotnet add package Poss.Win.Automation.Input
```

## Quick Start

```csharp
using Poss.Win.Automation.Input;

var sim = new InputSimulator();
sim.SendText("Hello, World!");
sim.Click(100, 200, "LButton");
sim.Send("Ctrl down");
sim.Send("A");
sim.Send("Ctrl up");

// Optional: restrict input to a specific app (e.g. Notepad)
var simFiltered = new InputSimulator("notepad");
simFiltered.SendText("Only when Notepad is focused");
```

## Main types

- **InputSimulator** — `SendText`, `Send`, `Click`, `MouseWheel`, `MouseSetPos`, `MouseDeltaMove`, `MouseGetPos`, `WinActivate`, `GetWindowTitle`, `GetWindowPos`, etc.

## Requirements

- .NET Standard 2.0
- Windows (uses Win32 `SendInput`, etc.)

## License

MIT. See [LICENSE](https://github.com/latur-h/Poss.Win.Automation/blob/main/LICENSE) in the repository.

## Repository

[https://github.com/latur-h/Poss.Win.Automation](https://github.com/latur-h/Poss.Win.Automation)
