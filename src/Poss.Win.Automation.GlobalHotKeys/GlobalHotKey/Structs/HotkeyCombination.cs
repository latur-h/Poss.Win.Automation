using System;
using System.Collections.Generic;
using System.Linq;
using Poss.Win.Automation.Common.Keys.Enums;
using Poss.Win.Automation.Common.Structs;

namespace Poss.Win.Automation.GlobalHotKeys.Structs
{
    /// <summary>
    /// Immutable set of key strokes that define a hotkey combination.
    /// Supports different key states (Down, Up, Press) per key.
    /// </summary>
    public readonly struct HotkeyCombination : IEquatable<HotkeyCombination>
    {
        private readonly KeyStroke[] _strokes;
        private readonly bool _hasUpTrigger;

        /// <summary>
        /// The key strokes that define this combination.
        /// </summary>
        public IReadOnlyList<KeyStroke> Strokes => _strokes ?? Array.Empty<KeyStroke>();

        /// <summary>
        /// Number of key strokes in the combination.
        /// </summary>
        public int Count => _strokes?.Length ?? 0;

        /// <summary>
        /// True if the combination has no strokes.
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// True if the combination has an Up stroke (trigger-on-release).
        /// </summary>
        public bool HasUpTrigger => _hasUpTrigger;

        internal HashSet<VirtualKey> GetKeys()
        {
            if (_strokes == null || _strokes.Length == 0)
                return new HashSet<VirtualKey>();

            return new HashSet<VirtualKey>(_strokes.Select(s => s.Key));
        }

        /// <summary>
        /// Creates a combination from the specified key strokes.
        /// </summary>
        public HotkeyCombination(params KeyStroke[] strokes)
        {
            if (strokes == null || strokes.Length == 0)
            {
                _strokes = Array.Empty<KeyStroke>();
                _hasUpTrigger = false;

                return;
            }

            _strokes = strokes
                .Where(s => s.Key != VirtualKey.None)
                .Distinct()
                .OrderBy(s => s.Key)
                .ThenBy(s => s.Action)
                .ToArray();

            _hasUpTrigger = Array.Exists(_strokes, s => s.Action == KeyAction.Up);
        }

        /// <summary>
        /// Creates a combination from the specified key strokes.
        /// </summary>
        public HotkeyCombination(IEnumerable<KeyStroke> strokes)
        {
            if (strokes == null)
            {
                _strokes = Array.Empty<KeyStroke>();
                _hasUpTrigger = false;

                return;
            }

            _strokes = strokes
                .Where(s => s.Key != VirtualKey.None)
                .Distinct()
                .OrderBy(s => s.Key)
                .ThenBy(s => s.Action)
                .ToArray();

            _hasUpTrigger = Array.Exists(_strokes, s => s.Action == KeyAction.Up);
        }

        /// <summary>
        /// Parses a key combination string. Format: "Ctrl + A Up", "Shift + LButton".
        /// </summary>
        /// <param name="keysString">Key combination string. Parts separated by '+', optional action per key: Up, Down, Press.</param>
        /// <param name="result">The parsed combination when successful.</param>
        /// <returns>True if parsing succeeded; otherwise, false.</returns>
        public static bool TryParse(string keysString, out HotkeyCombination result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(keysString)) return false;

            var parts = keysString
                .Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();

            if (parts.Length == 0) return false;

            var strokes = new List<KeyStroke>(parts.Length);
            foreach (var part in parts)
            {
                if (!KeyStroke.TryParse(part, out var stroke)) return false;

                strokes.Add(stroke);
            }

            result = new HotkeyCombination(strokes);
            return true;
        }

        /// <summary>
        /// Parses a key combination string.
        /// </summary>
        /// <param name="keysString">Key combination string.</param>
        /// <returns>The parsed combination.</returns>
        /// <exception cref="ArgumentException">Thrown when the string is invalid.</exception>
        public static HotkeyCombination Parse(string keysString)
        {
            if (!TryParse(keysString, out var result))
                throw new ArgumentException($"Invalid key combination: {keysString}", nameof(keysString));

            return result;
        }

        internal bool Matches(KeyStroke currentStroke, HashSet<VirtualKey> pressedKeys)
        {
            if (pressedKeys == null || _strokes == null || _strokes.Length == 0) return false;

            foreach (var stroke in _strokes)
            {
                if (stroke.Action == KeyAction.Up)
                {
                    if (currentStroke.Action != KeyAction.Up) return false;
                    if (!KeysMatch(stroke.Key, currentStroke.Key)) return false;
                }
                else
                {
                    if (!IsModifierOrKeyPressed(stroke.Key, pressedKeys)) return false;
                }
            }

            return true;
        }

        private static bool IsModifierOrKeyPressed(VirtualKey required, HashSet<VirtualKey> pressedKeys)
        {
            if (pressedKeys.Contains(required)) return true;
            if (required == VirtualKey.Ctrl || required == VirtualKey.Control)
                return pressedKeys.Contains(VirtualKey.LControl) || pressedKeys.Contains(VirtualKey.RControl);
            if (required == VirtualKey.Shift)
                return pressedKeys.Contains(VirtualKey.LShift) || pressedKeys.Contains(VirtualKey.RShift);
            if (required == VirtualKey.Alt)
                return pressedKeys.Contains(VirtualKey.LAlt) || pressedKeys.Contains(VirtualKey.RAlt);
            if (required == VirtualKey.Win)
                return pressedKeys.Contains(VirtualKey.LWin) || pressedKeys.Contains(VirtualKey.RWin);

            return false;
        }

        internal static bool KeysMatch(VirtualKey a, VirtualKey b)
        {
            if (a == b) return true;

            return IsModifierEquivalent(a, b);
        }

        private static bool IsModifierEquivalent(VirtualKey a, VirtualKey b)
        {
            var ctrl = new[] { VirtualKey.Ctrl, VirtualKey.Control, VirtualKey.LControl, VirtualKey.RControl };
            var shift = new[] { VirtualKey.Shift, VirtualKey.LShift, VirtualKey.RShift };
            var alt = new[] { VirtualKey.Alt, VirtualKey.LAlt, VirtualKey.RAlt };
            var win = new[] { VirtualKey.Win, VirtualKey.LWin, VirtualKey.RWin };

            return InGroup(a, b, ctrl) || InGroup(a, b, shift) || InGroup(a, b, alt) || InGroup(a, b, win);
        }

        private static bool InGroup(VirtualKey a, VirtualKey b, VirtualKey[] group)
        {
            bool hasA = false, hasB = false;

            for (int i = 0; i < group.Length; i++)
            {
                if (group[i] == a) hasA = true;
                if (group[i] == b) hasB = true;
            }

            return hasA && hasB;
        }

        public bool Equals(HotkeyCombination other) => SequenceEqual(_strokes, other._strokes);

        public override bool Equals(object obj) => obj is HotkeyCombination other && Equals(other);

        public override int GetHashCode()
        {
            if (_strokes == null || _strokes.Length == 0) return 0;

            unchecked
            {
                int hash = 17;

                foreach (var s in _strokes)
                {
                    hash = hash * 31 + (int)s.Key;
                    hash = hash * 31 + (int)s.Action;
                }

                return hash;
            }
        }

        public override string ToString() => string.Join(" + ", Strokes.Select(s => s.ToString()));

        private static bool SequenceEqual(KeyStroke[] a, KeyStroke[] b)
        {
            if (a == b) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i].Key != b[i].Key || a[i].Action != b[i].Action) return false;

            return true;
        }
    }
}
