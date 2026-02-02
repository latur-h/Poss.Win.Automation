using System.Runtime.InteropServices;

namespace Poss.Win.Automation.Native.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }
}