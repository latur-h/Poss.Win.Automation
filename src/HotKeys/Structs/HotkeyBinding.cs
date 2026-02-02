using System;
using Poss.Win.Automation.Common.Keys.Enums;
using Poss.Win.Automation.Common.Structs;

namespace Poss.Win.Automation.HotKeys.Structs
{
    /// <summary>
    /// Represents a registered hotkey binding (id and combination). Action is stored internally.
    /// </summary>
    public readonly struct HotkeyBinding : IEquatable<HotkeyBinding>
    {
        public string Id { get; }
        public HotkeyCombination Combination { get; }

        public HotkeyBinding(string id, HotkeyCombination combination)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Combination = combination;
        }

        public HotkeyBinding(string id, params KeyStroke[] strokes)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Combination = new HotkeyCombination(strokes);
        }

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
