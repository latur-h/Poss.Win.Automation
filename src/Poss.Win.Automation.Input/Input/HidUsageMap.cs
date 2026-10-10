namespace Poss.Win.Automation.Input
{
    internal static class HidUsageMap
    {
        internal static bool TryGetUsage(ushort virtualKey, out ushort usage)
        {
            if (virtualKey >= 0x41 && virtualKey <= 0x5A)
            {
                usage = (ushort)(0x04 + (virtualKey - 0x41));
                return true;
            }

            if (virtualKey >= 0x31 && virtualKey <= 0x39)
            {
                usage = (ushort)(0x1E + (virtualKey - 0x31));
                return true;
            }

            if (virtualKey >= 0x70 && virtualKey <= 0x7B)
            {
                usage = (ushort)(0x3A + (virtualKey - 0x70));
                return true;
            }

            if (virtualKey >= 0x7C && virtualKey <= 0x87)
            {
                usage = (ushort)(0x68 + (virtualKey - 0x7C));
                return true;
            }

            if (virtualKey >= 0x61 && virtualKey <= 0x69)
            {
                usage = (ushort)(0x59 + (virtualKey - 0x61));
                return true;
            }

            switch (virtualKey)
            {
                case 0x30: usage = 0x27; return true;
                case 0x0D: usage = 0x28; return true;
                case 0x1B: usage = 0x29; return true;
                case 0x08: usage = 0x2A; return true;
                case 0x09: usage = 0x2B; return true;
                case 0x20: usage = 0x2C; return true;
                case 0xBD: usage = 0x2D; return true;
                case 0xBB: usage = 0x2E; return true;
                case 0xDB: usage = 0x2F; return true;
                case 0xDD: usage = 0x30; return true;
                case 0xDC: usage = 0x31; return true;
                case 0xBA: usage = 0x33; return true;
                case 0xDE: usage = 0x34; return true;
                case 0xC0: usage = 0x35; return true;
                case 0xBC: usage = 0x36; return true;
                case 0xBE: usage = 0x37; return true;
                case 0xBF: usage = 0x38; return true;
                case 0x14: usage = 0x39; return true;
                case 0x2C: usage = 0x46; return true;
                case 0x91: usage = 0x47; return true;
                case 0x13: usage = 0x48; return true;
                case 0x2D: usage = 0x49; return true;
                case 0x24: usage = 0x4A; return true;
                case 0x21: usage = 0x4B; return true;
                case 0x2E: usage = 0x4C; return true;
                case 0x23: usage = 0x4D; return true;
                case 0x22: usage = 0x4E; return true;
                case 0x27: usage = 0x4F; return true;
                case 0x25: usage = 0x50; return true;
                case 0x28: usage = 0x51; return true;
                case 0x26: usage = 0x52; return true;
                case 0x90: usage = 0x53; return true;
                case 0x6F: usage = 0x54; return true;
                case 0x6A: usage = 0x55; return true;
                case 0x6D: usage = 0x56; return true;
                case 0x6B: usage = 0x57; return true;
                case 0x60: usage = 0x62; return true;
                case 0x6E: usage = 0x63; return true;
                case 0x5D: usage = 0x65; return true;
                case 0x6C: usage = 0x85; return true;
                case 0x10: usage = 0xE1; return true;
                case 0x11: usage = 0xE0; return true;
                case 0x12: usage = 0xE2; return true;
                case 0xA0: usage = 0xE1; return true;
                case 0xA1: usage = 0xE5; return true;
                case 0xA2: usage = 0xE0; return true;
                case 0xA3: usage = 0xE4; return true;
                case 0xA4: usage = 0xE2; return true;
                case 0xA5: usage = 0xE6; return true;
                case 0x5B: usage = 0xE3; return true;
                case 0x5C: usage = 0xE7; return true;
                default:
                    usage = 0;
                    return false;
            }
        }
    }
}
