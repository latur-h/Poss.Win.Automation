# Poss.Win.Automation.GlobalHotKeys

**Global hotkey** registration and management for Windows: system-wide keyboard and mouse hotkeys. Part of the **Poss.Win.Automation** family. Targets .NET Standard 2.0.

Use this package when you only need global hotkeys (no input simulation). If you need both input and hotkeys, use the umbrella package **Poss.Win.Automation**.

## Installation

```bash
dotnet add package Poss.Win.Automation.GlobalHotKeys
```

## Quick Start

```csharp
using Poss.Win.Automation.GlobalHotKeys;

// Console apps: enable message loop so hooks work
var manager = new GlobalGlobalHotKeyManager(new GlobalHotKeyManagerOptions { RunMessageLoop = true });
manager.Start();

manager.Register("save", async () => { /* save */ }, "Ctrl+S");
manager.Register("onRelease", async () => { /* on key up */ }, "F5 Up");

// manager.Stop(); manager.Dispose();
```

## Main types

- **GlobalGlobalHotKeyManager** — `Start`, `Stop`, `Register`, `Unregister`, `Change`, `GetRegisteredHotkeys`
- **GlobalHotKeyManagerOptions** — e.g. `RunMessageLoop = true` for console apps

## Requirements

- .NET Standard 2.0
- Windows (uses Win32 low-level hooks). A **message loop** is required; in console apps set `RunMessageLoop = true`.

## License

MIT. See [LICENSE](https://github.com/latur-h/Poss.Win.Automation/blob/main/LICENSE) in the repository.

## Repository

[https://github.com/latur-h/Poss.Win.Automation](https://github.com/latur-h/Poss.Win.Automation)
