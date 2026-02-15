using System.Runtime.InteropServices;

namespace Poss.Win.Automation.Native.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;
        public INPUTUNION U;
    }
}