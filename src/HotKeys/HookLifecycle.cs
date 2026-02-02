using System;
using System.Diagnostics;
using Poss.Win.Automation.Native;
using Poss.Win.Automation.Native.Constants;

namespace Poss.Win.Automation.HotKeys
{
    internal sealed class HookLifecycle : IDisposable
    {
        private readonly object _lock = new object();
        private readonly LowLevelProc _keyboardProc;
        private readonly LowLevelProc _mouseProc;

        private IntPtr _keyboardHookId = IntPtr.Zero;
        private IntPtr _mouseHookId = IntPtr.Zero;

        public bool IsRunning { get; private set; }

        public IntPtr KeyboardHookId => _keyboardHookId;
        public IntPtr MouseHookId => _mouseHookId;

        public HookLifecycle(LowLevelProc keyboardCallback, LowLevelProc mouseCallback)
        {
            _keyboardProc = keyboardCallback ?? throw new ArgumentNullException(nameof(keyboardCallback));
            _mouseProc = mouseCallback ?? throw new ArgumentNullException(nameof(mouseCallback));
        }

        public void Start()
        {
            lock (_lock)
            {
                if (IsRunning)
                    return;

                try
                {
                    _keyboardHookId = SetHook(_keyboardProc, HookConstants.WH_KEYBOARD_LL);
                    if (_keyboardHookId == IntPtr.Zero)
                        throw new InvalidOperationException("Failed to install keyboard hook");

                    _mouseHookId = SetHook(_mouseProc, HookConstants.WH_MOUSE_LL);
                    if (_mouseHookId == IntPtr.Zero)
                    {
                        User32.UnhookWindowsHookEx(_keyboardHookId);
                        _keyboardHookId = IntPtr.Zero;
                        throw new InvalidOperationException("Failed to install mouse hook");
                    }

                    IsRunning = true;
                }
                catch
                {
                    UnhookBoth();
                    throw;
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!IsRunning)
                    return;

                UnhookBoth();
                IsRunning = false;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!IsRunning)
                    return;

                UnhookBoth();
                IsRunning = false;
            }
        }

        private void UnhookBoth()
        {
            if (_keyboardHookId != IntPtr.Zero)
            {
                User32.UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }

            if (_mouseHookId != IntPtr.Zero)
            {
                User32.UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }
        }

        private static IntPtr SetHook(LowLevelProc proc, int hookType)
        {
            var moduleHandle = Kernel32.GetModuleHandle(null);

            if (moduleHandle == IntPtr.Zero)
            {
                try
                {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    var moduleName = System.IO.Path.GetFileName(assembly.Location);
                    moduleHandle = Kernel32.GetModuleHandle(moduleName);
                }
                catch
                {
                    using (var process = Process.GetCurrentProcess())
                    {
                        moduleHandle = Kernel32.GetModuleHandle(process.ProcessName);
                    }
                }
            }

            if (moduleHandle == IntPtr.Zero)
                throw new InvalidOperationException("Could not obtain module handle for hook installation");

            return User32.SetWindowsHookEx(hookType, proc, moduleHandle, 0);
        }
    }
}
