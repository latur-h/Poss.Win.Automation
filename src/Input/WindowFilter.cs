namespace Poss.Win.Automation.Input
{
    /// <summary>
    /// Registration filter for input: match by process name or window title. Nullable for quick "no filter" checks.
    /// </summary>
    public sealed class WindowFilter
    {
        /// <summary>Process name (no .exe) or window title substring to match.</summary>
        public string Name { get; }

        /// <summary>Whether to match by process name or by window title.</summary>
        public WindowFilterKind Type { get; }

        /// <summary>
        /// Creates a filter with the given name and match type.
        /// </summary>
        public WindowFilter(string name, WindowFilterKind type)
        {
            Name = name;
            Type = type;
        }
    }
}
