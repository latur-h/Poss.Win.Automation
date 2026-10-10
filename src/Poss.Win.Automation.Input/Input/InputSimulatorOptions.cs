namespace Poss.Win.Automation.Input
{
    /// <summary>
    /// Configuration options for <see cref="InputSimulator"/>.
    /// </summary>
    public sealed class InputSimulatorOptions
    {
        /// <summary>
        /// When true, Send, Click, MouseWheel, and MouseDeltaMove go through the virtual keyboard and mouse.
        /// SendText, MouseSetPos, SetCapsLock, and SetNumLock stay on the Win32 path.
        /// The driver must already be installed.
        /// </summary>
        public bool UseVirtualDevice { get; set; }
    }
}
