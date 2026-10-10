using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Poss.Win.Automation.VirtualHid
{
    /// <summary>
    /// One attachment to the shared virtual keyboard and mouse.
    /// Dispose closes this attachment. The devices stay up while another session is open.
    /// </summary>
    public sealed class VirtualHidSession : IDisposable
    {
        private readonly object _gate = new object();
        private readonly VirtualHidDeviceHandle _handle;
        private readonly byte[] _token;
        private bool _disposed;

        private VirtualHidSession(VirtualHidDeviceHandle handle, byte[] token)
        {
            _handle = handle;
            _token = token;
        }

        /// <summary>
        /// Opens the control device and completes hello.
        /// </summary>
        /// <exception cref="InvalidOperationException">The driver is not installed or hello was rejected.</exception>
        /// <exception cref="Win32Exception">CreateFile or DeviceIoControl failed.</exception>
        public static VirtualHidSession Open()
        {
            VirtualHidDeviceHandle handle = NativeMethods.CreateFile(
                VhfProtocol.UserDevicePath,
                NativeMethods.GenericRead | NativeMethods.GenericWrite,
                NativeMethods.FileShareRead | NativeMethods.FileShareWrite,
                IntPtr.Zero,
                NativeMethods.OpenExisting,
                NativeMethods.FileAttributeNormal,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                handle.Dispose();
                if (error == 2 || error == 3)
                {
                    throw new InvalidOperationException(
                        "Virtual HID driver is not available. Install PossWinAutomationVhf and start its device.",
                        new Win32Exception(error));
                }

                throw new Win32Exception(error);
            }

            try
            {
                var request = new VhfHelloRequest { Version = VhfProtocol.Version };
                VhfHelloResponse response = Transact<VhfHelloRequest, VhfHelloResponse>(
                    handle,
                    VhfProtocol.IoctlHello,
                    request,
                    VhfProtocol.HelloRequestSize,
                    VhfProtocol.HelloResponseSize);

                if (response.Version != VhfProtocol.Version || response.Token == null || response.Token.Length != VhfProtocol.TokenSize)
                    throw new InvalidOperationException("Virtual HID hello returned an unexpected response.");

                var token = new byte[VhfProtocol.TokenSize];
                Buffer.BlockCopy(response.Token, 0, token, 0, VhfProtocol.TokenSize);
                return new VirtualHidSession(handle, token);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Sends a HID keyboard usage from page 0x07. Modifiers are 0xE0 through 0xE7.
        /// </summary>
        public void SendKey(ushort usage, bool down)
        {
            if (usage == 0 || usage > 0xFF)
                throw new ArgumentOutOfRangeException(nameof(usage));

            Send(VhfProtocol.IoctlKey, new VhfKeyRequest
            {
                Token = CopyToken(),
                Usage = usage,
                Down = down ? VhfProtocol.KeyDown : VhfProtocol.KeyUp
            }, VhfProtocol.KeyRequestSize);
        }

        /// <summary>
        /// Sends a mouse button down or up. Button is one of the <see cref="VhfProtocol"/> mouse button values.
        /// </summary>
        public void SendMouseButton(byte button, bool down)
        {
            if (button > VhfProtocol.MouseButtonX2)
                throw new ArgumentOutOfRangeException(nameof(button));

            Send(VhfProtocol.IoctlMouseButton, new VhfMouseButtonRequest
            {
                Token = CopyToken(),
                Button = button,
                Down = down ? VhfProtocol.KeyDown : VhfProtocol.KeyUp
            }, VhfProtocol.MouseButtonRequestSize);
        }

        /// <summary>
        /// Sends a relative mouse move.
        /// </summary>
        public void SendMouseMove(int deltaX, int deltaY)
        {
            Send(VhfProtocol.IoctlMouseMove, new VhfMouseMoveRequest
            {
                Token = CopyToken(),
                DeltaX = deltaX,
                DeltaY = deltaY
            }, VhfProtocol.MouseMoveRequestSize);
        }

        /// <summary>
        /// Sends a mouse wheel change. Delta uses the Windows notch unit, typically 120.
        /// </summary>
        public void SendMouseWheel(int delta)
        {
            Send(VhfProtocol.IoctlMouseWheel, new VhfMouseWheelRequest
            {
                Token = CopyToken(),
                Delta = delta
            }, VhfProtocol.MouseWheelRequestSize);
        }

        /// <summary>
        /// Closes this attachment.
        /// </summary>
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _handle.Dispose();
            }
        }

        private byte[] CopyToken()
        {
            var copy = new byte[VhfProtocol.TokenSize];
            lock (_gate)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(VirtualHidSession));

                Buffer.BlockCopy(_token, 0, copy, 0, VhfProtocol.TokenSize);
            }

            return copy;
        }

        private void Send<TRequest>(uint ioctl, TRequest request, int inputSize) where TRequest : struct
        {
            lock (_gate)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(VirtualHidSession));

                Transact<TRequest, byte>(_handle, ioctl, request, inputSize, 0);
            }
        }

        private static TResponse Transact<TRequest, TResponse>(
            VirtualHidDeviceHandle handle,
            uint ioctl,
            TRequest request,
            int inputSize,
            int outputSize)
            where TRequest : struct
        {
            if (Marshal.SizeOf(typeof(TRequest)) != inputSize)
                throw new InvalidOperationException("Virtual HID request layout does not match the driver contract.");

            IntPtr input = Marshal.AllocHGlobal(inputSize);
            IntPtr output = outputSize > 0 ? Marshal.AllocHGlobal(outputSize) : IntPtr.Zero;
            try
            {
                Marshal.StructureToPtr(request, input, false);
                bool ok = NativeMethods.DeviceIoControl(
                    handle,
                    ioctl,
                    input,
                    (uint)inputSize,
                    output,
                    (uint)outputSize,
                    out uint returned,
                    IntPtr.Zero);

                if (!ok)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (outputSize == 0)
                    return default;

                if (returned < (uint)outputSize)
                    throw new InvalidOperationException("Virtual HID driver returned a short response.");

                object response = Marshal.PtrToStructure(output, typeof(TResponse));
                if (response == null)
                    throw new InvalidOperationException("Virtual HID driver returned an empty response.");

                return (TResponse)response;
            }
            finally
            {
                Marshal.FreeHGlobal(input);
                if (output != IntPtr.Zero)
                    Marshal.FreeHGlobal(output);
            }
        }
    }
}
