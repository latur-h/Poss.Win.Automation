namespace Poss.Win.Automation.Native.Constants
{
    internal static class InputConstants
    {
        // --------------------
        // Input types (for INPUT.type)
        // --------------------
        internal const int INPUT_KEYBOARD = 1;
        internal const int INPUT_MOUSE = 0;

        // --------------------
        // Mouse dwFlags (for MOUSEINPUT.dwFlags)
        // --------------------
        internal const uint MOUSEEVENTF_MOVE = 0x0001;
        internal const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        internal const uint MOUSEEVENTF_LEFTUP = 0x0004;
        internal const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        internal const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        internal const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        internal const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        internal const uint MOUSEEVENTF_XDOWN = 0x0080;
        internal const uint MOUSEEVENTF_XUP = 0x0100;
        internal const uint MOUSEEVENTF_WHEEL = 0x0800;
        internal const uint MOUSEEVENTF_HWHEEL = 0x1000;

        // --------------------
        // Keyboard dwFlags (for KEYBDINPUT.dwFlags)
        // --------------------
        internal const uint KEYEVENTF_KEYUP = 0x0002;
        internal const uint KEYEVENTF_UNICODE = 0x0004;

        // --------------------
        // XButton identifiers (for MOUSEINPUT.mouseData when dwFlags = XDOWN or XUP)
        // --------------------
        internal const uint XBUTTON1 = 0x0001;
        internal const uint XBUTTON2 = 0x0002;
    }
}