using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Blockganizer
{
    internal static class DesktopWorkArea
    {
        public static Thickness PaddingFor(Window window, FrameworkElement viewport)
        {
            if (window.WindowState != WindowState.Maximized || PresentationSource.FromVisual(viewport) == null)
                return new Thickness(0);

            IntPtr monitor = MonitorFromWindow(new WindowInteropHelper(window).Handle, 2);
            var info = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return new Thickness(0);

            // Win32 gives screen pixels; the viewport uses WPF layout units.
            Point start = viewport.PointFromScreen(new Point(info.Work.Left, info.Work.Top));
            Point end = viewport.PointFromScreen(new Point(info.Work.Right, info.Work.Bottom));
            return new Thickness(Math.Max(0, start.X), Math.Max(0, start.Y),
                Math.Max(0, viewport.ActualWidth - end.X), Math.Max(0, viewport.ActualHeight - end.Y));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor, Work;
            public int Flags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    }
}
