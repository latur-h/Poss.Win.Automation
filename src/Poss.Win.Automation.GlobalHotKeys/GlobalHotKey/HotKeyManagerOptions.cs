namespace Poss.Win.Automation.GlobalHotKeys
{
    /// <summary>
    /// Configuration options for <see cref="GlobalGlobalHotKeyManager"/>.
    /// </summary>
    public sealed class GlobalHotKeyManagerOptions
    {
        /// <summary>
        /// If true, spawns a dedicated thread with a Windows message loop so hooks work
        /// in console apps without WinForms/WPF. If false, assumes the current thread
        /// already has a message loop (e.g. WinForms/WPF).
        /// </summary>
        public bool RunMessageLoop { get; set; }
    }
}
