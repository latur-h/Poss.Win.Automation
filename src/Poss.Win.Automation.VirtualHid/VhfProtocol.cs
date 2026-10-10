using System;
using System.Runtime.InteropServices;

namespace Poss.Win.Automation.VirtualHid
{
    /// <summary>
    /// Wire contract for the virtual HID control device.
    /// Layout matches driver/Poss.Win.Automation.Vhf/inc/VhfIoctl.h. Pack is 1.
    /// </summary>
    public static class VhfProtocol
    {
        public const uint Version = 1;
        public const int TokenSize = 16;

        public const byte KeyUp = 0;
        public const byte KeyDown = 1;

        public const byte MouseButtonLeft = 0;
        public const byte MouseButtonRight = 1;
        public const byte MouseButtonMiddle = 2;
        public const byte MouseButtonX1 = 3;
        public const byte MouseButtonX2 = 4;

        public const string UserDevicePath = @"\\.\PossWinAutomationVhf";

        public static readonly Guid DeviceInterface = new Guid("2d27e086-51b5-4e66-add5-8d7fa2c8a589");

        private const uint DeviceType = 0x8000;
        private const uint MethodBuffered = 0;
        private const uint FileAnyAccess = 0;

        public const uint IoctlHello = (DeviceType << 16) | (FileAnyAccess << 14) | (0x800u << 2) | MethodBuffered;
        public const uint IoctlKey = (DeviceType << 16) | (FileAnyAccess << 14) | (0x801u << 2) | MethodBuffered;
        public const uint IoctlMouseButton = (DeviceType << 16) | (FileAnyAccess << 14) | (0x802u << 2) | MethodBuffered;
        public const uint IoctlMouseMove = (DeviceType << 16) | (FileAnyAccess << 14) | (0x803u << 2) | MethodBuffered;
        public const uint IoctlMouseWheel = (DeviceType << 16) | (FileAnyAccess << 14) | (0x804u << 2) | MethodBuffered;

        public const int HelloRequestSize = 4;
        public const int HelloResponseSize = 20;
        public const int KeyRequestSize = 19;
        public const int MouseButtonRequestSize = 18;
        public const int MouseMoveRequestSize = 24;
        public const int MouseWheelRequestSize = 20;
    }

    /// <summary>
    /// Hello input. Version must be <see cref="VhfProtocol.Version"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhfHelloRequest
    {
        public uint Version;
    }

    /// <summary>
    /// Hello output. Token is valid only for the handle that completed hello.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhfHelloResponse
    {
        public uint Version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = VhfProtocol.TokenSize)]
        public byte[] Token;
    }

    /// <summary>
    /// Keyboard usage from HID page 0x07. Down is <see cref="VhfProtocol.KeyDown"/> or <see cref="VhfProtocol.KeyUp"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhfKeyRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = VhfProtocol.TokenSize)]
        public byte[] Token;

        public ushort Usage;
        public byte Down;
    }

    /// <summary>
    /// Mouse button. Button is one of the <see cref="VhfProtocol"/> mouse button values.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhfMouseButtonRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = VhfProtocol.TokenSize)]
        public byte[] Token;

        public byte Button;
        public byte Down;
    }

    /// <summary>
    /// Relative mouse movement.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhfMouseMoveRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = VhfProtocol.TokenSize)]
        public byte[] Token;

        public int DeltaX;
        public int DeltaY;
    }

    /// <summary>
    /// Mouse wheel. Delta uses the Windows notch unit, typically 120.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VhfMouseWheelRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = VhfProtocol.TokenSize)]
        public byte[] Token;

        public int Delta;
    }
}
