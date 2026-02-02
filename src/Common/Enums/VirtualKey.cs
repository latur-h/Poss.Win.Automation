namespace Poss.Win.Automation.Common.Keys.Enums
{
    public enum VirtualKey : ushort
    {
        // --------------------
        None = 0x00,
        // --------------------
        // Letters
        // --------------------
        A = 0x41,
        B = 0x42,
        C = 0x43,
        D = 0x44,
        E = 0x45,
        F = 0x46,
        G = 0x47,
        H = 0x48,
        I = 0x49,
        J = 0x4A,
        K = 0x4B,
        L = 0x4C,
        M = 0x4D,
        N = 0x4E,
        O = 0x4F,
        P = 0x50,
        Q = 0x51,
        R = 0x52,
        S = 0x53,
        T = 0x54,
        U = 0x55,
        V = 0x56,
        W = 0x57,
        X = 0x58,
        Y = 0x59,
        Z = 0x5A,
        // --------------------
        // Numbers (Top Row)
        // --------------------
        D0 = 0x30,
        D1 = 0x31,
        D2 = 0x32,
        D3 = 0x33,
        D4 = 0x34,
        D5 = 0x35,
        D6 = 0x36,
        D7 = 0x37,
        D8 = 0x38,
        D9 = 0x39,
        // --------------------
        // Function Keys
        // --------------------
        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B,
        F13 = 0x7C,
        F14 = 0x7D,
        F15 = 0x7E,
        F16 = 0x7F,
        F17 = 0x80,
        F18 = 0x81,
        F19 = 0x82,
        F20 = 0x83,
        F21 = 0x84,
        F22 = 0x85,
        F23 = 0x86,
        F24 = 0x87,
        // --------------------
        // Modifier Keys
        // --------------------
        Shift = 0x10,
        Control = 0x11,
        Alt = 0x12,
        LShift = 0xA0,
        RShift = 0xA1,
        LControl = 0xA2,
        RControl = 0xA3,
        LCtrl = 0xA2,
        RCtrl = 0xA3,
        LAlt = 0xA4,
        RAlt = 0xA5,
        LWin = 0x5B,
        RWin = 0x5C,
        Apps = 0x5D,
        // --------------------
        // Lock Keys
        // --------------------
        CapsLock = 0x14,
        NumLock = 0x90,
        ScrollLock = 0x91,
        // --------------------
        // Navigation Keys
        // --------------------
        Enter = 0x0D,
        Escape = 0x1B,
        Tab = 0x09,
        Pause = 0x13,
        PrintScreen = 0x2C,
        Space = 0x20,
        Backspace = 0x08,
        Insert = 0x2D,
        Delete = 0x2E,
        Home = 0x24,
        End = 0x23,
        PageUp = 0x21,
        PageDown = 0x22,
        ArrowLeft = 0x25,
        ArrowUp = 0x26,
        ArrowRight = 0x27,
        ArrowDown = 0x28,
        // --------------------
        // Numpad
        // --------------------
        NumPad0 = 0x60,
        NumPad1 = 0x61,
        NumPad2 = 0x62,
        NumPad3 = 0x63,
        NumPad4 = 0x64,
        NumPad5 = 0x65,
        NumPad6 = 0x66,
        NumPad7 = 0x67,
        NumPad8 = 0x68,
        NumPad9 = 0x69,
        NumPadMultiply = 0x6A,
        NumPadAdd = 0x6B,
        NumPadSeparator = 0x6C,
        NumPadSubtract = 0x6D,
        NumPadDecimal = 0x6E,
        NumPadDivide = 0x6F,
        // --------------------
        // Symbols
        // --------------------
        Semicolon = 0xBA,
        Equal = 0xBB,
        Comma = 0xBC,
        Minus = 0xBD,
        Period = 0xBE,
        Slash = 0xBF,
        Backtick = 0xC0,
        OpenBracket = 0xDB,
        Backslash = 0xDC,
        CloseBracket = 0xDD,
        Quote = 0xDE,
        // --------------------
        // Mouse buttons
        // --------------------
        LButton = 0x01,
        RButton = 0x02,
        Cancel = 0x03,
        MButton = 0x04,
        XButton1 = 0x05,
        XButton2 = 0x06,
        // --------------------
        // Control keys (misc)
        // --------------------
        Clear = 0x0C,
        Select = 0x29,
        Print = 0x2A,
        Execute = 0x2B,
        Help = 0x2F,
        // --------------------
        // Modifier aliases
        // --------------------
        Ctrl = 0x11,
        Esc = 0x1B,
        Win = 0x5B,
        // --------------------
        // Arrow aliases
        // --------------------
        Left = 0x25,
        Up = 0x26,
        Right = 0x27,
        Down = 0x28,
        // --------------------
        // Numpad aliases
        // --------------------
        Num0 = 0x60,
        Num1 = 0x61,
        Num2 = 0x62,
        Num3 = 0x63,
        Num4 = 0x64,
        Num5 = 0x65,
        Num6 = 0x66,
        Num7 = 0x67,
        Num8 = 0x68,
        Num9 = 0x69,
        Multiply = 0x6A,
        Add = 0x6B,
        Separator = 0x6C,
        Subtract = 0x6D,
        Decimal = 0x6E,
        Divide = 0x6F,
        // --------------------
        // Browser & Media keys
        // --------------------
        BrowserBack = 0xA6,
        BrowserForward = 0xA7,
        BrowserRefresh = 0xA8,
        BrowserStop = 0xA9,
        BrowserSearch = 0xAA,
        BrowserFavorites = 0xAB,
        BrowserHome = 0xAC,
        VolumeMute = 0xAD,
        VolumeDown = 0xAE,
        VolumeUp = 0xAF,
        MediaNext = 0xB0,
        MediaPrev = 0xB1,
        MediaStop = 0xB2,
        MediaPlayPause = 0xB3,
        LaunchMail = 0xB4,
        LaunchMediaSelect = 0xB5,
        LaunchApp1 = 0xB6,
        LaunchApp2 = 0xB7,
    }
}