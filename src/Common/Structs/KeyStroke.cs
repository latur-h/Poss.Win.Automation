using System;
using System.Collections.Generic;
using System.Linq;
using Poss.Win.Automation.Common.Keys.Enums;

namespace Poss.Win.Automation.Common.Structs
{
    public readonly struct KeyStroke
    {
        public readonly VirtualKey Key;
        public readonly KeyAction Action;

        private static readonly Dictionary<string, VirtualKey> StringToKey = new Dictionary<string, VirtualKey>(StringComparer.OrdinalIgnoreCase)
        {
            ["0"] = VirtualKey.D0,
            ["1"] = VirtualKey.D1,
            ["2"] = VirtualKey.D2,
            ["3"] = VirtualKey.D3,
            ["4"] = VirtualKey.D4,
            ["5"] = VirtualKey.D5,
            ["6"] = VirtualKey.D6,
            ["7"] = VirtualKey.D7,
            ["8"] = VirtualKey.D8,
            ["9"] = VirtualKey.D9,
            [";"] = VirtualKey.Semicolon,
            ["="] = VirtualKey.Equal,
            [","] = VirtualKey.Comma,
            ["-"] = VirtualKey.Minus,
            ["."] = VirtualKey.Period,
            ["/"] = VirtualKey.Slash,
            ["`"] = VirtualKey.Backtick,
            ["["] = VirtualKey.OpenBracket,
            ["\\"] = VirtualKey.Backslash,
            ["]"] = VirtualKey.CloseBracket,
            ["'"] = VirtualKey.Quote,
        };

        private static readonly Dictionary<string, KeyAction> StringToAction = new Dictionary<string, KeyAction>(StringComparer.OrdinalIgnoreCase)
        {
            ["click"] = KeyAction.Press,
            ["press"] = KeyAction.Press,
        };

        public KeyStroke(VirtualKey key, KeyAction action = KeyAction.Press)
        {
            Key = key;
            Action = action;
        }

        public KeyStroke(string input)
        {
            if (!TryParse(input, out KeyStroke result)) throw new ArgumentException($"Invalid key stroke: {input}");

            Key = result.Key;
            Action = result.Action;
        }

        public static bool TryParse(string input, out KeyStroke result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(input)) return false;

            var parts = input
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            if (parts.Length == 0 || parts.Length > 2) return false;

            VirtualKey key;
            if (Enum.TryParse(parts[0], ignoreCase: true, out key))
            {
                if (key == VirtualKey.None) return false;
            }
            else if (StringToKey.TryGetValue(parts[0], out key)) { } // mapped
            else if (parts[0].Length == 1)
            {
                char c = parts[0][0];
                if (char.IsDigit(c))
                    key = (VirtualKey)(0x30 + (c - '0'));
                else if (char.IsLetter(c))
                    key = (VirtualKey)char.ToUpperInvariant(c);
                else
                    return false;
            }
            else
            {
                return false;
            }

            KeyAction action = KeyAction.Press;
            if (parts.Length == 2)
            {
                if (StringToAction.TryGetValue(parts[1], out KeyAction mappedAction))
                    action = mappedAction;
                else if (!Enum.TryParse(parts[1], ignoreCase: true, out action)) return false;
            }

            result = new KeyStroke(key, action);
            return true;
        }

        public static bool TryGetVirtualKeyCode(string key, out ushort vkCode)
        {
            vkCode = 0;
            if (string.IsNullOrWhiteSpace(key)) return false;

            VirtualKey vk;
            if (Enum.TryParse(key, ignoreCase: true, out vk) && vk != VirtualKey.None)
            {
                vkCode = (ushort)vk;

                return true;
            }
            if (StringToKey.TryGetValue(key, out vk))
            {
                vkCode = (ushort)vk;

                return true;
            }
            if (key.Length == 1)
            {
                char c = key[0];
                if (char.IsDigit(c))
                {
                    vkCode = (ushort)(0x30 + (c - '0'));

                    return true;
                }
                if (char.IsLetter(c))
                {
                    vkCode = (ushort)char.ToUpperInvariant(c);

                    return true;
                }
            }

            return false;
        }

        public override string ToString() => Action == KeyAction.Press ? Key.ToString() : $"{Key} {Action}";
    }
}
