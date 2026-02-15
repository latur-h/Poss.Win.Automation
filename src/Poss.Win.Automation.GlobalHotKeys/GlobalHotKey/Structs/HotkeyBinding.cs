using System;
using Poss.Win.Automation.Common.Keys.Enums;
using Poss.Win.Automation.Common.Structs;

namespace Poss.Win.Automation.GlobalHotKeys.Structs
{
    /// <summary>
    /// Represents a registered hotkey binding (id and combination).
    /// </summary>
    public readonly struct HotkeyBinding : IEquatable<HotkeyBinding>
    {
        /// <summary>
        /// Unique identifier for the binding.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The key combination that triggers the binding.
        /// </summary>
        public HotkeyCombination Combination { get; }

        /// <summary>
        /// Creates a binding with the specified id and combination.
        /// </summary>
        public HotkeyBinding(string id, HotkeyCombination combination)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Combination = combination;
        }

        /// <summary>
        /// Creates a binding with the specified id and key strokes.
        /// </summary>
        public HotkeyBinding(string id, params KeyStroke[] strokes)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Combination = new HotkeyCombination(strokes);
        }

        /// <summary>
        /// Creates a binding with the specified id and key combination string.
        /// </summary>
        public HotkeyBinding(string id, string keysString)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Combination = HotkeyCombination.Parse(keysString);
        }

        public bool Equals(HotkeyBinding other) =>
            string.Equals(Id, other.Id, StringComparison.Ordinal) && Combination.Equals(other.Combination);

        public override bool Equals(object obj) => obj is HotkeyBinding other && Equals(other);

        public override int GetHashCode() =>
            unchecked((Id != null ? StringComparer.Ordinal.GetHashCode(Id) : 0) * 31 + Combination.GetHashCode());

        public override string ToString() => $"{Id}: {Combination}";
    }
}
