using System;

namespace Poss.Win.Automation.Input
{
    /// <summary>
    /// Identifies the foreground window by handle and process ID. Used when caching match results
    /// so the same (hWnd, pid) is not recomputed if the process was killed and the handle reused.
    /// </summary>
    public readonly struct ForegroundIdentity
    {
        /// <summary>Window handle.</summary>
        public IntPtr Hwnd { get; }

        /// <summary>Process ID of the window.</summary>
        public uint Pid { get; }

        /// <summary>
        /// Creates an identity from the given window handle and process ID.
        /// </summary>
        public ForegroundIdentity(IntPtr hwnd, uint pid)
        {
            Hwnd = hwnd;
            Pid = pid;
        }

        /// <summary>
        /// Returns true when both Hwnd and Pid match.
        /// </summary>
        public bool Equals(ForegroundIdentity other) => Hwnd == other.Hwnd && Pid == other.Pid;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ForegroundIdentity other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (Hwnd.GetHashCode() * 397) ^ Pid.GetHashCode();
    }
}
