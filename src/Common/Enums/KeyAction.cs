namespace Poss.Win.Automation.Common.Keys.Enums
{
    /// <summary>
    /// Key state for a stroke: press (down+up), down only, or up only.
    /// </summary>
    public enum KeyAction
    {
        /// <summary>Press and release (default for hotkey combinations).</summary>
        Press,

        /// <summary>Key down only.</summary>
        Down,

        /// <summary>Key up only (trigger-on-release).</summary>
        Up
    }
}