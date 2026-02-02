using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Poss.Win.Automation.Common.Keys.Enums;
using Poss.Win.Automation.Common.Structs;
using Poss.Win.Automation.Native;
using Poss.Win.Automation.Native.Constants;
using Poss.Win.Automation.Native.Structs;

namespace Poss.Win.Automation.Input
{
    /// <summary>
    /// Simulates keyboard and mouse input, including key presses, mouse clicks,
    /// cursor movement, and window queries. Supports optional filtering by process or window title.
    /// </summary>
    public sealed class InputSimulator
    {
        private static readonly ConcurrentDictionary<string, KeyStroke> _cache = new ConcurrentDictionary<string, KeyStroke>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<ushort> _heldModifiers = new HashSet<ushort>();
        private readonly string _process;

        /// <summary>
        /// Initializes a new instance without process filtering. All input is simulated regardless of foreground window.
        /// </summary>
        public InputSimulator()
        {
            _process = null;
        }

        /// <summary>
        /// Initializes a new instance with filtering for a specific process or window title.
        /// Input is simulated only when the foreground window matches.
        /// </summary>
        /// <param name="process">Process name (with or without ".exe") or window title substring to restrict input to.</param>
        public InputSimulator(string process)
        {
            if (process != null && process.Contains(".exe"))
                process = process.Substring(0, process.IndexOf(".exe", StringComparison.OrdinalIgnoreCase)).Trim();

            _process = process;
        }

        /// <summary>
        /// Sends text as Unicode characters without translating to keycodes. Reliable for non-ASCII and accented characters.
        /// </summary>
        /// <param name="text">The text to send. Supports full Unicode including surrogate pairs.</param>
        public void SendText(string text)
        {
            if (ShouldSkipInput()) return;
            if (string.IsNullOrEmpty(text)) return;

            var inputs = new List<INPUT>();
            foreach (char c in text)
            {
                ushort scan = (ushort)c;
                inputs.Add(new INPUT
                {
                    type = InputConstants.INPUT_KEYBOARD,
                    U = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = scan,
                            dwFlags = InputConstants.KEYEVENTF_UNICODE,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                });
                inputs.Add(new INPUT
                {
                    type = InputConstants.INPUT_KEYBOARD,
                    U = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = scan,
                            dwFlags = InputConstants.KEYEVENTF_UNICODE | InputConstants.KEYEVENTF_KEYUP,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                });
            }
            if (inputs.Count > 0)
                User32.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Clicks at the specified screen coordinates.
        /// </summary>
        /// <param name="x">X-coordinate on screen.</param>
        /// <param name="y">Y-coordinate on screen.</param>
        /// <param name="button">Mouse button: "LButton", "RButton", "MButton", "XButton1", "XButton2". Default: LButton.</param>
        /// <param name="count">Number of clicks. Default: 1.</param>
        /// <exception cref="ArgumentException">Thrown when the button name is unknown or not a mouse button.</exception>
        public void Click(int x, int y, string button = "LButton", int count = 1)
        {
            if (ShouldSkipInput()) return;

            if (!TryGetOrParse(button, out KeyStroke stroke) || !IsMouseKey(stroke.Key))
                throw new ArgumentException($"Unknown mouse button: {button}");

            MouseSetPos(x, y);
            for (int i = 0; i < count; i++)
            {
                SendMouse(stroke.Key, true);
                SendMouse(stroke.Key, false);
            }
        }

        /// <summary>
        /// Sends a mouse wheel event. Positive delta = scroll up, negative = scroll down.
        /// </summary>
        /// <param name="delta">Scroll amount. Typically 120 per notch. Use positive for up, negative for down.</param>
        public void MouseWheel(int delta)
        {
            if (ShouldSkipInput()) return;

            var input = new INPUT
            {
                type = InputConstants.INPUT_MOUSE,
                U = new INPUTUNION
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = InputConstants.MOUSEEVENTF_WHEEL,
                        mouseData = (uint)delta,
                        dx = 0,
                        dy = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            User32.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Sends a simulated key or mouse input from a string. Format: "[key] [action]", e.g. "A down", "LButton click".
        /// </summary>
        /// <param name="input">The input string. Key name plus optional action: "down", "up", or "click"/"press" (default).</param>
        /// <exception cref="ArgumentException">Thrown when the key name is unknown.</exception>
        public void Send(string input)
        {
            if (ShouldSkipInput()) return;
            if (string.IsNullOrWhiteSpace(input)) return;

            KeyStroke stroke = GetOrParse(input);
            Execute(stroke);
        }

        /// <summary>
        /// Sends a simulated key or mouse input from a <see cref="KeyStroke"/>.
        /// </summary>
        /// <param name="stroke">The key stroke with key and action (Press, Down, Up).</param>
        public void Send(KeyStroke stroke)
        {
            if (ShouldSkipInput()) return;
            Execute(stroke);
        }

        private static KeyStroke GetOrParse(string input)
        {
            if (TryGetOrParse(input, out KeyStroke stroke))
                return stroke;
            throw new ArgumentException($"Unknown key: {input}");
        }

        private static bool TryGetOrParse(string input, out KeyStroke stroke)
        {
            stroke = default;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string key = input.Trim();
            if (_cache.TryGetValue(key, out stroke))
                return true;
            if (!KeyStroke.TryParse(input, out stroke))
                return false;

            _cache.TryAdd(key, stroke);
            return true;
        }

        private void Execute(KeyStroke stroke)
        {
            ushort vkCode = (ushort)stroke.Key;

            if (IsMouseKey(stroke.Key))
            {
                switch (stroke.Action)
                {
                    case KeyAction.Down: SendMouse(stroke.Key, true); break;
                    case KeyAction.Up: SendMouse(stroke.Key, false); break;
                    default:
                        SendMouse(stroke.Key, true);
                        SendMouse(stroke.Key, false);
                        break;
                }
            }
            else
            {
                switch (stroke.Action)
                {
                    case KeyAction.Down:
                        if (!_heldModifiers.Contains(vkCode))
                        {
                            SendKey(vkCode, true);
                            if (IsModifierKey(vkCode))
                                _heldModifiers.Add(vkCode);
                        }
                        break;
                    case KeyAction.Up:
                        SendKey(vkCode, false);
                        if (IsModifierKey(vkCode))
                            _heldModifiers.Remove(vkCode);
                        break;
                    default:
                        SendKey(vkCode, true);
                        SendKey(vkCode, false);
                        break;
                }
            }
        }

        private void SendKey(ushort vkCode, bool isKeyDown)
        {
            var input = new INPUT
            {
                type = InputConstants.INPUT_KEYBOARD,
                U = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vkCode,
                        dwFlags = isKeyDown ? 0u : InputConstants.KEYEVENTF_KEYUP,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            User32.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SendMouse(VirtualKey button, bool down)
        {
            uint flags;
            uint data;

            switch (button)
            {
                case VirtualKey.LButton:
                    flags = down ? InputConstants.MOUSEEVENTF_LEFTDOWN : InputConstants.MOUSEEVENTF_LEFTUP;
                    data = 0;
                    break;
                case VirtualKey.RButton:
                    flags = down ? InputConstants.MOUSEEVENTF_RIGHTDOWN : InputConstants.MOUSEEVENTF_RIGHTUP;
                    data = 0;
                    break;
                case VirtualKey.MButton:
                    flags = down ? InputConstants.MOUSEEVENTF_MIDDLEDOWN : InputConstants.MOUSEEVENTF_MIDDLEUP;
                    data = 0;
                    break;
                case VirtualKey.XButton1:
                    flags = down ? InputConstants.MOUSEEVENTF_XDOWN : InputConstants.MOUSEEVENTF_XUP;
                    data = InputConstants.XBUTTON1;
                    break;
                case VirtualKey.XButton2:
                    flags = down ? InputConstants.MOUSEEVENTF_XDOWN : InputConstants.MOUSEEVENTF_XUP;
                    data = InputConstants.XBUTTON2;
                    break;
                default:
                    throw new ArgumentException($"Not a mouse button: {button}");
            }

            var input = new INPUT
            {
                type = InputConstants.INPUT_MOUSE,
                U = new INPUTUNION
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = flags,
                        mouseData = data,
                        dx = 0,
                        dy = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            User32.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Sets the mouse cursor position to the specified screen coordinates.
        /// </summary>
        /// <param name="x">The X-coordinate on screen.</param>
        /// <param name="y">The Y-coordinate on screen.</param>
        public void MouseSetPos(int x, int y)
        {
            if (ShouldSkipInput()) return;

            User32.SetCursorPos(x, y);
        }

        /// <summary>
        /// Smoothly moves the mouse cursor from the current position to the target with easing.
        /// </summary>
        /// <param name="targetX">Target X-coordinate.</param>
        /// <param name="targetY">Target Y-coordinate.</param>
        /// <param name="speed">Movement speed. Higher values = faster. Default: 1.0.</param>
        public void MouseDeltaMove(int targetX, int targetY, double speed = 1.0)
        {
            if (ShouldSkipInput()) return;
            if (speed <= 0) speed = 1.0;

            User32.GetCursorPos(out POINT start);
            int totalDx = targetX - start.x;
            int totalDy = targetY - start.y;

            double distance = Math.Sqrt(totalDx * totalDx + totalDy * totalDy);
            int durationMs = (int)(distance * 0.8 / speed);
            int steps = Math.Max(10, durationMs / 10);
            var rand = new Random();
            double curveStrength = rand.NextDouble() * 0.5 + 0.5;
            double prevX = 0, prevY = 0;

            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double easeT = t * t * (3 - 2 * t);
                double curve = Math.Sin(easeT * Math.PI) * curveStrength;

                double x = (totalDx * easeT) + curve * 10;
                double y = (totalDy * easeT) + curve * 5;

                int dx = (int)Math.Round(x - prevX);
                int dy = (int)Math.Round(y - prevY);

                prevX += dx;
                prevY += dy;

                if (dx != 0 || dy != 0)
                    SendDelta(dx, dy);

                Thread.Sleep((int)(10 / speed) + rand.Next(3));
            }
        }

        private void SendDelta(int dx, int dy)
        {
            var input = new INPUT[1];

            input[0].type = InputConstants.INPUT_MOUSE;
            input[0].U.mi.dx = dx;
            input[0].U.mi.dy = dy;
            input[0].U.mi.mouseData = 0;
            input[0].U.mi.dwFlags = InputConstants.MOUSEEVENTF_MOVE;
            input[0].U.mi.time = 0;
            input[0].U.mi.dwExtraInfo = IntPtr.Zero;

            User32.SendInput(1, input, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Gets the current mouse cursor position.
        /// </summary>
        /// <returns>The cursor position as a <see cref="POINT"/> with x and y coordinates.</returns>
        public POINT MouseGetPos()
        {
            User32.GetCursorPos(out POINT pt);

            return pt;
        }

        /// <summary>
        /// Gets the process ID of the currently focused window.
        /// </summary>
        /// <returns>The PID, or null if no window is focused.</returns>
        public uint? GetWindowPID()
        {
            var hWnd = User32.GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return null;

            User32.GetWindowThreadProcessId(hWnd, out uint pid);
            return pid;
        }

        /// <summary>
        /// Gets the process name of the currently focused window.
        /// </summary>
        /// <returns>The process name, or null if no window is focused or the process cannot be resolved.</returns>
        public string GetWindowProcessName()
        {
            var hWnd = User32.GetForegroundWindow();

            return hWnd == IntPtr.Zero ? null : GetProcessNameFromHandle(hWnd);
        }

        /// <summary>
        /// Activates the first window matching the given process name or window title.
        /// </summary>
        /// <param name="query">Process name (e.g. "exe notepad") or window title substring. Null or empty = no-op.</param>
        /// <returns>True if a matching window was found and activated; otherwise, false.</returns>
        public bool WinActivate(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;

            var hWnd = FindMatchingWindow(query);
            if (hWnd == IntPtr.Zero) return false;

            return User32.SetForegroundWindow(hWnd);
        }

        /// <summary>
        /// Gets the title of the specified window, or the foreground window if null.
        /// </summary>
        /// <param name="hWnd">Window handle. Null = foreground window.</param>
        /// <returns>The window title, or null if no window.</returns>
        public string GetWindowTitle(IntPtr? hWnd = null)
        {
            var wnd = hWnd ?? User32.GetForegroundWindow();
            if (wnd == IntPtr.Zero) return null;

            var sb = new StringBuilder(256);
            User32.GetWindowText(wnd, sb, sb.Capacity);

            return sb.ToString();
        }

        /// <summary>
        /// Gets the position and size of the specified window, or the foreground window if null.
        /// </summary>
        /// <param name="hWnd">Window handle. Null = foreground window.</param>
        /// <returns>A <see cref="RECT"/> with Left, Top, Right, Bottom; or null if no window.</returns>
        public RECT? GetWindowPos(IntPtr? hWnd = null)
        {
            var wnd = hWnd ?? User32.GetForegroundWindow();
            if (wnd == IntPtr.Zero) return null;

            if (!User32.GetWindowRect(wnd, out RECT rect)) return null;
            return rect;
        }

        /// <summary>
        /// Gets the window class name of the specified window, or the foreground window if null.
        /// </summary>
        /// <param name="hWnd">Window handle. Null = foreground window.</param>
        /// <returns>The window class name, or null if no window.</returns>
        public string GetWindowClass(IntPtr? hWnd = null)
        {
            var wnd = hWnd ?? User32.GetForegroundWindow();
            if (wnd == IntPtr.Zero) return null;

            var sb = new StringBuilder(256);
            return User32.GetClassName(wnd, sb, sb.Capacity) > 0 ? sb.ToString() : null;
        }

        /// <summary>
        /// Sets the CapsLock state.
        /// </summary>
        /// <param name="on">True to turn on, false to turn off.</param>
        public void SetCapsLock(bool on)
        {
            if (ShouldSkipInput()) return;

            bool current = (User32.GetAsyncKeyState(0x14) & 1) != 0;
            if (current != on)
            {
                SendKey(0x14, true);
                SendKey(0x14, false);
            }
        }

        /// <summary>
        /// Sets the NumLock state.
        /// </summary>
        /// <param name="on">True to turn on, false to turn off.</param>
        public void SetNumLock(bool on)
        {
            if (ShouldSkipInput()) return;

            bool current = (User32.GetAsyncKeyState(0x90) & 1) != 0;
            if (current != on)
            {
                SendKey(0x90, true);
                SendKey(0x90, false);
            }
        }

        /// <summary>
        /// Blocks or unblocks user input from the keyboard and mouse.
        /// </summary>
        /// <param name="block">True to block input, false to unblock.</param>
        public static void BlockInput(bool block)
        {
            User32.BlockInput(block);
        }

        /// <summary>
        /// Checks if the currently focused window matches the given process name or window title.
        /// </summary>
        /// <param name="query">Process name (e.g. "exe notepad") or window title substring.</param>
        /// <returns>True if the current window matches; otherwise, false.</returns>
        public bool IsActiveWindow(string query)
        {
            var hWnd = User32.GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return false;

            if (query.StartsWith("exe ", StringComparison.OrdinalIgnoreCase))
            {
                string targetExe = query.Substring(4).Trim();
                var processName = GetProcessNameFromHandle(hWnd);
                return processName != null && string.Equals(processName, targetExe, StringComparison.OrdinalIgnoreCase);
            }

            return WindowTitleContains(hWnd, query);
        }

        /// <summary>
        /// Checks if the currently focused window matches the process or title specified during construction.
        /// </summary>
        /// <returns>True if the current window matches; otherwise, false. Returns false if no process filter was set.</returns>
        public bool IsActiveWindow() => !string.IsNullOrEmpty(_process) && IsActiveWindow(_process);

        /// <summary>
        /// Determines whether the specified key is currently held down.
        /// </summary>
        /// <param name="vKey">The key name (e.g. "Ctrl", "A", "F5").</param>
        /// <returns>True if the key is down; otherwise, false. Returns false for unknown keys.</returns>
        public bool GetKeyState(string vKey)
        {
            if (!TryGetOrParse(vKey, out KeyStroke stroke)) return false;

            ushort vkCode = (ushort)stroke.Key;
            return (User32.GetAsyncKeyState(vkCode) & 0x8000) != 0;
        }

        private bool ShouldSkipInput() => !string.IsNullOrEmpty(_process) && !IsActiveWindow(_process);

        private static IntPtr FindMatchingWindow(string query)
        {
            IntPtr result = IntPtr.Zero;
            var criteria = new WindowSearchCriteria(query);

            bool Callback(IntPtr hWnd, IntPtr lParam)
            {
                if (criteria.IsExe)
                {
                    var name = GetProcessNameFromHandle(hWnd);
                    if (name != null && string.Equals(name, criteria.Target, StringComparison.OrdinalIgnoreCase))
                    {
                        result = hWnd;

                        return false;
                    }
                }
                else
                {
                    var sb = new StringBuilder(256);
                    User32.GetWindowText(hWnd, sb, sb.Capacity);
                    if (sb.ToString().IndexOf(criteria.Target, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result = hWnd;

                        return false;
                    }
                }

                return true;
            }

            User32.EnumWindows(Callback, IntPtr.Zero);
            return result;
        }

        private static string GetProcessNameFromHandle(IntPtr hWnd)
        {
            User32.GetWindowThreadProcessId(hWnd, out uint pid);
            try
            {
                return Process.GetProcessById((int)pid).ProcessName;
            }
            catch
            {
                return null;
            }
        }

        private static bool WindowTitleContains(IntPtr hWnd, string query)
        {
            var sb = new StringBuilder(256);

            User32.GetWindowText(hWnd, sb, sb.Capacity);

            return sb.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMouseKey(VirtualKey key) =>
            key == VirtualKey.LButton || key == VirtualKey.RButton || key == VirtualKey.MButton ||
            key == VirtualKey.XButton1 || key == VirtualKey.XButton2;

        private static bool IsModifierKey(ushort vkCode) =>
            vkCode == 0x10 || vkCode == 0x11 || vkCode == 0x12 || vkCode == 0x5B || vkCode == 0x5C;

        private class WindowSearchCriteria
        {
            public readonly bool IsExe;
            public readonly string Target;

            public WindowSearchCriteria(string query)
            {
                if (query.StartsWith("exe ", StringComparison.OrdinalIgnoreCase))
                {
                    IsExe = true;
                    Target = query.Substring(4).Trim();
                }
                else
                {
                    IsExe = false;
                    Target = query;
                }
            }
        }
    }
}
