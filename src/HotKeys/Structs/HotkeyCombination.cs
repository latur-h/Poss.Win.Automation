using System;
using System.Collections.Generic;
using System.Linq;
using Poss.Win.Automation.Common.Keys.Enums;
using Poss.Win.Automation.Common.Structs;

namespace Poss.Win.Automation.HotKeys.Structs
{
    /// <summary>
    /// Immutable set of key strokes that define a hotkey combination.
    /// Supports different key states (Down, Up, Press) per key.
    /// </summary>
    public readonly struct HotkeyCombination : IEquatable<HotkeyCombination>
    {
        private readonly KeyStroke[] _strokes;
        private readonly bool _hasUpTrigger;

        public IReadOnlyList<KeyStroke> Strokes => _strokes ?? Array.Empty<KeyStroke>();

        public int Count => _strokes?.Length ?? 0;

        public bool IsEmpty => Count == 0;

        /// <summary>
        /// True if the combination has an Up stroke (trigger-on-release). Precomputed at init.
        /// </summary>
        public bool HasUpTrigger => _hasUpTrigger;

        /// <summary>
        /// Returns the set of all keys in the combination (for active tracking).
        /// </summary>
        public HashSet<VirtualKey> GetKeys()
        {
            if (_strokes == null || _strokes.Length == 0)
                return new HashSet<VirtualKey>();
            return new HashSet<VirtualKey>(_strokes.Select(s => s.Key));
        }

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
        /// Parses a key combination string. Format: "A + H Up + d down + LButton"
        /// Parts are split by '+' and each part is parsed by KeyStroke.TryParse.
        /// </summary>
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
        /// Parses a key combination string. Throws on invalid input.
        /// </summary>
        public static HotkeyCombination Parse(string keysString)
        {
            if (!TryParse(keysString, out var result))
                throw new ArgumentException($"Invalid key combination: {keysString}", nameof(keysString));
            return result;
        }

        /// <summary>
        /// Matches against current stroke (for Up triggers) and pressed keys (for Down/Press).
        /// </summary>
        public bool Matches(KeyStroke currentStroke, HashSet<VirtualKey> pressedKeys)
        {
            if (pressedKeys == null || _strokes == null || _strokes.Length == 0) return false;

            foreach (var stroke in _strokes)
            {
                if (stroke.Action == KeyAction.Up)
                {
                    if (currentStroke.Key != stroke.Key || currentStroke.Action != KeyAction.Up) return false;
                }
                else
                {
                    if (!pressedKeys.Contains(stroke.Key)) return false;
                }
            }

            return true;
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
