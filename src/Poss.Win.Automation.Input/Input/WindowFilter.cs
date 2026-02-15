namespace Poss.Win.Automation.Input
{
    /// <summary>
    /// Filter for input: match by process name or window title. Used in collections to restrict input to one or more targets.
    /// </summary>
    public readonly struct WindowFilter
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
