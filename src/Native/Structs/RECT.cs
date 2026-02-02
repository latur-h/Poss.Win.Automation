using System.Runtime.InteropServices;

namespace Poss.Win.Automation.Native.Structs
{
    /// <summary>
    /// Rectangle bounds. Maps to Win32 RECT.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        /// <summary>Left edge.</summary>
        public int Left;

        /// <summary>Top edge.</summary>
        public int Top;

        /// <summary>Right edge.</summary>
        public int Right;

        /// <summary>Bottom edge.</summary>
        public int Bottom;
    }
}