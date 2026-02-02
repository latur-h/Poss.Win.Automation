namespace Poss.Win.Automation.Native.Constants
{
    internal static class HookConstants
    {
        // --------------------
        // Hook types (for SetWindowsHookEx)
        // --------------------
        internal const int WH_KEYBOARD_LL = 13;
        internal const int WH_MOUSE_LL = 14;

        // --------------------
        // Keyboard messages (wParam in LowLevelKeyboardProc)
        // --------------------
        internal const int WM_KEYDOWN = 0x0100;
        internal const int WM_KEYUP = 0x0101;
        internal const int WM_SYSKEYDOWN = 0x0104;
        internal const int WM_SYSKEYUP = 0x0105;

        // --------------------
        // Mouse messages (wParam in LowLevelMouseProc)
        // --------------------
        internal const int WM_LBUTTONDOWN = 0x0201;
        internal const int WM_LBUTTONUP = 0x0202;
        internal const int WM_RBUTTONDOWN = 0x0204;
        internal const int WM_RBUTTONUP = 0x0205;
        internal const int WM_MBUTTONDOWN = 0x0207;
        internal const int WM_MBUTTONUP = 0x0208;
        internal const int WM_XBUTTONDOWN = 0x020B;
        internal const int WM_XBUTTONUP = 0x020C;

        // --------------------
        // Message loop
        // --------------------
        internal const uint WM_QUIT = 0x0012;
    }
}
