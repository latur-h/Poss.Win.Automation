using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Poss.Win.Automation.Common.Keys.Enums;
using Poss.Win.Automation.Common.Structs;
using Poss.Win.Automation.GlobalHotKeys.Structs;
using Poss.Win.Automation.Native;
using Poss.Win.Automation.Native.Constants;
using Poss.Win.Automation.Native.Structs;

namespace Poss.Win.Automation.GlobalHotKeys
{
    /// <summary>
    /// Facade for global hotkey handling. Coordinates hook lifecycle and hotkey registration.
    /// </summary>
    public sealed class GlobalHotKeyManager : IDisposable
    {
        private readonly object _lock = new object();
        private readonly HookLifecycle _hookLifecycle;
        private readonly GlobalHotKeys _hotKeys;
        private readonly HashSet<VirtualKey> _pressedInputs = new HashSet<VirtualKey>();
        private readonly bool _runMessageLoop;
        private Thread _messageLoopThread;
        private uint _messageLoopThreadId;
        private bool _disposed;

        /// <summary>
        /// Creates a new <see cref="GlobalHotKeyManager"/> with default options.
        /// </summary>
        public GlobalHotKeyManager()
        {
            _runMessageLoop = false;
            _hookLifecycle = new HookLifecycle(KeyboardProc, MouseProc);
            _hotKeys = new GlobalHotKeys();
        }

        /// <summary>
        /// Creates a new <see cref="GlobalHotKeyManager"/> with the specified options.
        /// </summary>
        /// <param name="options">Optional configuration. If null, defaults are used.</param>
        public GlobalHotKeyManager(GlobalHotKeyManagerOptions options)
        {
            _runMessageLoop = options?.RunMessageLoop ?? false;
            _hookLifecycle = new HookLifecycle(KeyboardProc, MouseProc);
            _hotKeys = new GlobalHotKeys();
        }

        /// <summary>
        /// Starts the hotkey manager. Uses options passed to the constructor.
        /// When <see cref="GlobalHotKeyManagerOptions.RunMessageLoop"/> is true, spawns a dedicated thread with a Windows message loop
        /// so hooks work in console apps without WinForms/WPF.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the manager has been disposed.</exception>
        public void Start()
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(GlobalHotKeyManager));

                if (_hookLifecycle.IsRunning)
                    return;

                if (_runMessageLoop)
                {
                    _messageLoopThread = new Thread(MessageLoopThread)
                    {
                        IsBackground = true,
                        Name = "GlobalHotKeyMessageLoop"
                    };
                    _messageLoopThread.SetApartmentState(ApartmentState.STA);
                    _messageLoopThread.Start();
                    while (!_hookLifecycle.IsRunning && _messageLoopThread.IsAlive)
                        Thread.Sleep(1);
                }
                else
                {
                    _hookLifecycle.Start();
                }
            }
        }

        /// <summary>
        /// Stops the hotkey manager and releases hooks. Idempotent.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (!_hookLifecycle.IsRunning)
                    return;

                _pressedInputs.Clear();

                if (_messageLoopThread != null && _messageLoopThread.IsAlive)
                {
                    User32.PostThreadMessage(_messageLoopThreadId, HookConstants.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                    _messageLoopThread.Join(5000);
                    _messageLoopThread = null;
                }
                else
                {
                    _hookLifecycle.Stop();
                }
            }
        }

        private void MessageLoopThread()
        {
            _messageLoopThreadId = Kernel32.GetCurrentThreadId();
            _hookLifecycle.Start();

            var msg = new MSG();
            while (User32.GetMessage(ref msg, IntPtr.Zero, 0, 0) > 0)
            {
                User32.TranslateMessage(ref msg);
                User32.DispatchMessage(ref msg);
            }

            _hookLifecycle.Stop();
        }

        /// <summary>
        /// Gets whether the hotkey manager is currently running and capturing input.
        /// </summary>
        public bool IsRunning => _hookLifecycle.IsRunning;

        /// <summary>
        /// Registers a hotkey binding with the specified id.
        /// </summary>
        /// <param name="id">Unique identifier for the binding.</param>
        /// <param name="action">Async callback invoked when the hotkey is triggered.</param>
        /// <param name="strokes">Key strokes that define the combination (e.g. Ctrl + A).</param>
        /// <returns>The binding id.</returns>
        public string Register(string id, Func<Task> action, params KeyStroke[] strokes) =>
            _hotKeys.Register(id, action, strokes);

        /// <summary>
        /// Registers a hotkey binding with the specified id. Keys format: "Ctrl + A Up", "Shift + LButton".
        /// </summary>
        /// <param name="id">Unique identifier for the binding.</param>
        /// <param name="action">Async callback invoked when the hotkey is triggered.</param>
        /// <param name="keysString">Key combination string. Parts separated by '+', optional action per key: Up, Down, Press.</param>
        /// <returns>The binding id.</returns>
        public string Register(string id, Func<Task> action, string keysString) =>
            _hotKeys.Register(id, action, keysString);

        /// <summary>
        /// Registers a hotkey binding with an auto-generated id.
        /// </summary>
        /// <param name="action">Async callback invoked when the hotkey is triggered.</param>
        /// <param name="strokes">Key strokes that define the combination.</param>
        /// <returns>The auto-generated binding id.</returns>
        public string Register(Func<Task> action, params KeyStroke[] strokes) =>
            _hotKeys.Register(action, strokes);

        /// <summary>
        /// Registers a hotkey binding with an auto-generated id. Keys format: "Ctrl + A Up".
        /// </summary>
        /// <param name="action">Async callback invoked when the hotkey is triggered.</param>
        /// <param name="keysString">Key combination string.</param>
        /// <returns>The auto-generated binding id.</returns>
        public string Register(Func<Task> action, string keysString) =>
            _hotKeys.Register(action, keysString);

        /// <summary>
        /// Registers a hotkey binding asynchronously. Returns immediately with the id.
        /// </summary>
        public Task<string> RegisterAsync(string id, Func<Task> action, params KeyStroke[] strokes) =>
            _hotKeys.RegisterAsync(id, action, strokes);

        /// <summary>
        /// Registers a hotkey binding asynchronously with a key string. Returns immediately with the id.
        /// </summary>
        public Task<string> RegisterAsync(string id, Func<Task> action, string keysString) =>
            _hotKeys.RegisterAsync(id, action, keysString);

        /// <summary>
        /// Registers a hotkey binding asynchronously with auto-generated id. Returns immediately with the id.
        /// </summary>
        public Task<string> RegisterAsync(Func<Task> action, params KeyStroke[] strokes) =>
            _hotKeys.RegisterAsync(action, strokes);

        /// <summary>
        /// Registers a hotkey binding asynchronously with auto-generated id and key string. Returns immediately with the id.
        /// </summary>
        public Task<string> RegisterAsync(Func<Task> action, string keysString) =>
            _hotKeys.RegisterAsync(action, keysString);

        /// <summary>
        /// Unregisters a hotkey binding by id.
        /// </summary>
        /// <param name="id">The binding id to remove.</param>
        public void Unregister(string id) =>
            _hotKeys.Unregister(id);

        /// <summary>
        /// Unregisters a hotkey binding by id asynchronously.
        /// </summary>
        public Task UnregisterAsync(string id) =>
            _hotKeys.UnregisterAsync(id);

        /// <summary>
        /// Changes the key combination for an existing binding.
        /// </summary>
        public void Change(string id, params KeyStroke[] newStrokes) =>
            _hotKeys.Change(id, newStrokes);

        /// <summary>
        /// Changes the key combination for an existing binding using a key string.
        /// </summary>
        public void Change(string id, string newKeysString) =>
            _hotKeys.Change(id, newKeysString);

        /// <summary>
        /// Changes the action callback for an existing binding.
        /// </summary>
        public void Change(string id, Func<Task> newAction) =>
            _hotKeys.Change(id, newAction);

        /// <summary>
        /// Returns a copy of all registered hotkey bindings (id and combination only).
        /// </summary>
        public IReadOnlyList<HotkeyBinding> GetRegisteredHotkeys() =>
            _hotKeys.GetRegisteredHotkeys();

        /// <summary>
        /// Returns a copy of all registered hotkey bindings asynchronously.
        /// </summary>
        public Task<IReadOnlyList<HotkeyBinding>> GetRegisteredHotkeysAsync() =>
            _hotKeys.GetRegisteredHotkeysAsync();

        private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (VirtualKey)(ushort)(vkCode & 0xFFFF);

                var action = wParam == (IntPtr)HookConstants.WM_KEYDOWN || wParam == (IntPtr)HookConstants.WM_SYSKEYDOWN
                    ? KeyAction.Down
                    : KeyAction.Up;

                if (action == KeyAction.Down)
                    _pressedInputs.Add(key);
                else
                    _pressedInputs.Remove(key);

                FireToWorker(new KeyStroke(key, action));
            }

            return User32.CallNextHookEx(_hookLifecycle.KeyboardHookId, nCode, wParam, lParam);
        }

        private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = (int)wParam;
                VirtualKey? button = null;
                bool isDown = false;
                bool isUp = false;

                switch (msg)
                {
                    case HookConstants.WM_LBUTTONDOWN: isDown = true; button = VirtualKey.LButton; break;
                    case HookConstants.WM_RBUTTONDOWN: isDown = true; button = VirtualKey.RButton; break;
                    case HookConstants.WM_MBUTTONDOWN: isDown = true; button = VirtualKey.MButton; break;
                    case HookConstants.WM_XBUTTONDOWN:
                        isDown = true;
                        var hsDown = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        button = ((hsDown.mouseData >> 16) & 0xFFFF) == 1 ? VirtualKey.XButton1 : VirtualKey.XButton2;
                        break;
                    case HookConstants.WM_LBUTTONUP: isUp = true; button = VirtualKey.LButton; break;
                    case HookConstants.WM_RBUTTONUP: isUp = true; button = VirtualKey.RButton; break;
                    case HookConstants.WM_MBUTTONUP: isUp = true; button = VirtualKey.MButton; break;
                    case HookConstants.WM_XBUTTONUP:
                        isUp = true;
                        var hsUp = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        button = ((hsUp.mouseData >> 16) & 0xFFFF) == 1 ? VirtualKey.XButton1 : VirtualKey.XButton2;
                        break;
                }

                if (button.HasValue)
                {
                    if (isDown)
                        _pressedInputs.Add(button.Value);
                    else if (isUp)
                        _pressedInputs.Remove(button.Value);

                    FireToWorker(new KeyStroke(button.Value, isDown ? KeyAction.Down : KeyAction.Up));
                }
            }

            return User32.CallNextHookEx(_hookLifecycle.MouseHookId, nCode, wParam, lParam);
        }

        private void FireToWorker(KeyStroke stroke)
        {
            var snapshot = new HashSet<VirtualKey>(_pressedInputs);
            _ = _hotKeys.ProcessInputAsync(stroke, snapshot);
        }

        /// <summary>
        /// Releases resources and stops the hotkey manager. Idempotent.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _pressedInputs.Clear();
                _hookLifecycle.Dispose();
                _disposed = true;
            }
        }
    }
}
