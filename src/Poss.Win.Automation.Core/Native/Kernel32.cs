using System;
using System.Runtime.InteropServices;

namespace Poss.Win.Automation.Native
{
    internal static class Kernel32
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
    }
}