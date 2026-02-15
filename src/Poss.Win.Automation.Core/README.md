# Poss.Win.Automation.Core

Shared types and Win32 native bindings for the **Poss.Win.Automation** family. This package is intended for use by **Poss.Win.Automation.Input** and **Poss.Win.Automation.GlobalHotKeys**; you typically do not install it directly unless you are building on top of it.

## When to use this package

- You need the **common types** (e.g. `KeyStroke`, `VirtualKey`, `KeyAction`) and **native structs** (e.g. `POINT`, `RECT`) from the Poss.Win.Automation stack.
- You are authoring a library that extends or composes with Input or GlobalHotKeys.

For **keyboard/mouse input simulation**, use **Poss.Win.Automation.Input**.  
For **global hotkeys**, use **Poss.Win.Automation.GlobalHotKeys**.  
For **both**, use **Poss.Win.Automation** (umbrella package).

## Installation

```bash
dotnet add package Poss.Win.Automation.Core
```

## License

MIT. See [LICENSE](https://github.com/latur-h/Poss.Win.Automation/blob/main/LICENSE) in the repository.

## Repository

[https://github.com/latur-h/Poss.Win.Automation](https://github.com/latur-h/Poss.Win.Automation)
