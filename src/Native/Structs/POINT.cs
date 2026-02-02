using System.Runtime.InteropServices;

namespace Poss.Win.Automation.Native.Structs
{
    /// <summary>
    /// Screen coordinates. Maps to Win32 POINT.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        /// <summary>X-coordinate.</summary>
        public int x;

        /// <summary>Y-coordinate.</summary>
        public int y;
    }
}