namespace Poss.Win.Automation.HotKeys
{
    /// <summary>
    /// Configuration options for <see cref="GlobalHotKeyManager"/>.
    /// </summary>
    public sealed class HotKeyManagerOptions
    {
        /// <summary>
        /// If true, spawns a dedicated thread with a Windows message loop so hooks work
        /// in console apps without WinForms/WPF. If false, assumes the current thread
        /// already has a message loop (e.g. WinForms/WPF).
        /// </summary>
        public bool RunMessageLoop { get; set; }
    }
}
