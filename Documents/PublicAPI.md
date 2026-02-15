# Poss.Win.Automation — Public API Reference

## Namespaces

| Namespace | Description |
|-----------|-------------|
| `Poss.Win.Automation.GlobalHotKeys` | Global hotkey registration and management |
| `Poss.Win.Automation.Input` | Keyboard and mouse input simulation |
| `Poss.Win.Automation.Common.Keys.Enums` | Key action and virtual key enums |
| `Poss.Win.Automation.Common.Structs` | Key stroke struct |
| `Poss.Win.Automation.GlobalHotKeys.Structs` | Hotkey binding and combination structs |
| `Poss.Win.Automation.Native.Structs` | Win32 POINT and RECT structs |

---

## GlobalHotKeys

### GlobalHotKeyManager

| Member | Signature | Description |
|--------|-----------|-------------|
| Constructor | `GlobalHotKeyManager()` | Creates manager with default options |
| Constructor | `GlobalHotKeyManager(GlobalHotKeyManagerOptions options)` | Creates manager with specified options |
| Start | `void Start()` | Starts the hotkey manager and hooks |
| Stop | `void Stop()` | Stops the manager and releases hooks |
| IsRunning | `bool IsRunning` | Whether the manager is currently running |
| Register | `string Register(string id, Func<Task> action, params KeyStroke[] strokes)` | Registers hotkey by id and strokes |
| Register | `string Register(string id, Func<Task> action, string keysString)` | Registers hotkey by id and key string (e.g. "Ctrl+A Up") |
| Register | `string Register(Func<Task> action, params KeyStroke[] strokes)` | Registers hotkey with auto-generated id |
| Register | `string Register(Func<Task> action, string keysString)` | Registers hotkey with auto-generated id and key string |
| RegisterAsync | `Task<string> RegisterAsync(string id, Func<Task> action, params KeyStroke[] strokes)` | Async register by id |
| RegisterAsync | `Task<string> RegisterAsync(string id, Func<Task> action, string keysString)` | Async register by id and key string |
| RegisterAsync | `Task<string> RegisterAsync(Func<Task> action, params KeyStroke[] strokes)` | Async register with auto id |
| RegisterAsync | `Task<string> RegisterAsync(Func<Task> action, string keysString)` | Async register with auto id and key string |
| Unregister | `void Unregister(string id)` | Unregisters hotkey by id |
| UnregisterAsync | `Task UnregisterAsync(string id)` | Async unregister |
| Change | `void Change(string id, params KeyStroke[] newStrokes)` | Changes key combination for binding |
| Change | `void Change(string id, string newKeysString)` | Changes key combination by string |
| Change | `void Change(string id, Func<Task> newAction)` | Changes action callback |
| GetRegisteredHotkeys | `IReadOnlyList<HotkeyBinding> GetRegisteredHotkeys()` | Returns all registered bindings |
| GetRegisteredHotkeysAsync | `Task<IReadOnlyList<HotkeyBinding>> GetRegisteredHotkeysAsync()` | Async get registered hotkeys |
| Dispose | `void Dispose()` | Releases resources and stops manager |

### GlobalHotKeyManagerOptions

| Member | Type | Description |
|--------|------|-------------|
| RunMessageLoop | `bool` | If true, spawns a thread with Windows message loop for console apps |

---

## Input

### InputSimulator

| Member | Signature | Description |
|--------|-----------|-------------|
| Constructor | `InputSimulator()` | No process filtering |
| Constructor | `InputSimulator(params string[] processOrTitle)` | Filter by one or more process names or window titles; input is sent only when foreground matches any |
| Filters | `IReadOnlyList<WindowFilter> Filters` | The collection of process/window filters; empty when no filter is set |
| SendText | `void SendText(string text)` | Sends Unicode text |
| Send | `void Send(string input)` | Sends key/mouse from string (e.g. "A down", "LButton click") |
| Send | `void Send(KeyStroke stroke)` | Sends key stroke |
| Click | `void Click(int x, int y, string button = "LButton", int count = 1)` | Clicks at coordinates |
| MouseWheel | `void MouseWheel(int delta)` | Scroll wheel (positive = up) |
| MouseSetPos | `void MouseSetPos(int x, int y)` | Sets cursor position |
| MouseDeltaMove | `void MouseDeltaMove(int targetX, int targetY, double speed = 1.0)` | Smooth cursor movement |
| MouseGetPos | `POINT MouseGetPos()` | Gets cursor position |
| GetWindowPID | `uint? GetWindowPID()` | Gets PID of foreground window |
| GetWindowProcessName | `string GetWindowProcessName()` | Gets process name of foreground window |
| GetWindowTitle | `string GetWindowTitle(IntPtr? hWnd = null)` | Gets window title |
| GetWindowPos | `RECT? GetWindowPos(IntPtr? hWnd = null)` | Gets window bounds |
| GetWindowClass | `string GetWindowClass(IntPtr? hWnd = null)` | Gets window class name |
| WinActivate | `bool WinActivate(string query)` | Activates window by process/title |
| SetCapsLock | `void SetCapsLock(bool on)` | Sets CapsLock state |
| SetNumLock | `void SetNumLock(bool on)` | Sets NumLock state |
| BlockInput | `static void BlockInput(bool block)` | Blocks/unblocks user input |
| IsActiveWindow | `bool IsActiveWindow(string query)` | Checks if foreground matches query |
| IsActiveWindow | `bool IsActiveWindow()` | Checks if foreground matches any of the constructor filters |
| GetKeyState | `bool GetKeyState(string vKey)` | Returns true if key is held down |

### ForegroundIdentity (struct)

| Member | Type | Description |
|--------|------|-------------|
| Hwnd | `IntPtr` | Window handle |
| Pid | `uint` | Process ID of the window |
| Constructor | `ForegroundIdentity(IntPtr hwnd, uint pid)` | Creates identity from handle and PID |
| Equals | `bool Equals(ForegroundIdentity other)` | True when both Hwnd and Pid match |

### WindowFilter (struct)

| Member | Type | Description |
|--------|------|-------------|
| Name | `string` | Process name (no .exe) or window title substring to match |
| Type | `WindowFilterKind` | Process or Window |
| Constructor | `WindowFilter(string name, WindowFilterKind type)` | Creates filter |

### WindowFilterKind (enum)

| Value | Description |
|-------|-------------|
| Process | Match by process name |
| Window | Match by window title substring |

---

## Common.Keys.Enums

### KeyAction

| Value | Description |
|-------|-------------|
| Press | Press and release (default) |
| Down | Key down only |
| Up | Key up only (trigger-on-release) |

### VirtualKey

Windows virtual key codes (VK_*). Includes: A–Z, D0–D9, F1–F24, modifiers (Shift, Ctrl, Alt, Win), mouse (LButton, RButton, MButton, XButton1, XButton2), navigation keys, media keys, etc.

---

## Common.Structs

### KeyStroke

| Member | Type | Description |
|--------|------|-------------|
| Key | `VirtualKey` | Virtual key or mouse button |
| Action | `KeyAction` | Press, Down, or Up |
| Constructor | `KeyStroke(VirtualKey key, KeyAction action = KeyAction.Press)` | Creates stroke |
| Constructor | `KeyStroke(string input)` | Parses from string (e.g. "Ctrl Down", "LButton Up") |
| TryParse | `static bool TryParse(string input, out KeyStroke result)` | Parses key stroke string |
| TryGetVirtualKeyCode | `static bool TryGetVirtualKeyCode(string key, out ushort vkCode)` | Gets VK code for key name |

---

## GlobalHotKeys.Structs

### HotkeyBinding

| Member | Type | Description |
|--------|------|-------------|
| Id | `string` | Binding identifier |
| Combination | `HotkeyCombination` | Key combination |
| Constructor | `HotkeyBinding(string id, HotkeyCombination combination)` | Creates binding |
| Constructor | `HotkeyBinding(string id, params KeyStroke[] strokes)` | Creates from strokes |
| Constructor | `HotkeyBinding(string id, string keysString)` | Creates from key string |

### HotkeyCombination

| Member | Type | Description |
|--------|------|-------------|
| Strokes | `IReadOnlyList<KeyStroke>` | Key strokes in combination |
| Count | `int` | Number of strokes |
| IsEmpty | `bool` | True if no strokes |
| HasUpTrigger | `bool` | True if has Up stroke |
| Constructor | `HotkeyCombination(params KeyStroke[] strokes)` | Creates from strokes |
| Constructor | `HotkeyCombination(IEnumerable<KeyStroke> strokes)` | Creates from enumerable |
| TryParse | `static bool TryParse(string keysString, out HotkeyCombination result)` | Parses "Ctrl+A Up" format |
| Parse | `static HotkeyCombination Parse(string keysString)` | Parses or throws |

---

## Native.Structs

### POINT

| Member | Type | Description |
|--------|------|-------------|
| x | `int` | X-coordinate |
| y | `int` | Y-coordinate |

### RECT

| Member | Type | Description |
|--------|------|-------------|
| Left | `int` | Left edge |
| Top | `int` | Top edge |
| Right | `int` | Right edge |
| Bottom | `int` | Bottom edge |
