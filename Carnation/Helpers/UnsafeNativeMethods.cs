using System;
using System.Runtime.InteropServices;

namespace Carnation.Helpers
{
    internal static class UnsafeNativeMethods
    {
        internal const int LOGPIXELSX = 0x58;

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        internal static extern int GetDeviceCaps(IntPtr hDC, int index);
    }
}
