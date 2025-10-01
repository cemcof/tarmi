using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Betrian.WPF;

public static partial class NativeHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const string User32 = "user32.dll";
    private const string DWMApi = "dwmapi.dll";

    [LibraryImport(DWMApi, EntryPoint = "DwmSetWindowAttribute")]
    private static partial int DwmSetWindowStyle(IntPtr hwnd, int dwAttribute, [MarshalAs(UnmanagedType.Bool)] ref bool pvAttribute, int cbAttribute);

    public static bool TrySetDarkMode(Window window)
    {
        if (Environment.OSVersion.Version.Build < 22000)
        {
            return false;
        }

        WindowInteropHelper windowInteropHelper = new(window);
        nint windowHandle = windowInteropHelper.EnsureHandle();

        bool useDarkMode = true;
        return DwmSetWindowStyle(windowHandle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, Marshal.SizeOf<bool>()) >= 0;
    }
    public static Point GetMousePosition()
    {
        GetCursorPos(out Win32Point point);
        return new Point(point.X, point.Y);
    }

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out Win32Point lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Point
    {
        public int X;
        public int Y;
    };
}
