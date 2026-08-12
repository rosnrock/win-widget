using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace WinWidget.Services;

public static class WindowBehaviorService
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20L;
    private const long WsExToolWindow = 0x80L;
    private const long WsExNoActivate = 0x08000000L;

    public static void ConfigureWidgetWindow(Window window)
    {
        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = true;
        window.Background = Brushes.Transparent;
        window.ShowInTaskbar = false;
        window.ResizeMode = ResizeMode.NoResize;
        window.SourceInitialized += (_, _) => AddExtendedStyles(window, WsExToolWindow);
    }

    public static void BeginDrag(Window window, MouseButtonEventArgs args, bool isLocked)
    {
        if (!isLocked && args.ChangedButton == MouseButton.Left)
            window.DragMove();
    }

    public static void SetClickThrough(Window window, bool enabled)
    {
        if (!window.IsInitialized)
        {
            window.SourceInitialized += (_, _) => SetClickThrough(window, enabled);
            return;
        }

        var handle = new WindowInteropHelper(window).Handle;
        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        var mask = WsExTransparent | WsExNoActivate;
        style = enabled ? style | mask : style & ~mask;
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style));
    }

    private static void AddExtendedStyles(Window window, long styles)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var current = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(current | styles));
    }

    private static IntPtr GetWindowLongPtr(IntPtr window, int index) =>
        IntPtr.Size == 8 ? GetWindowLongPtr64(window, index) : new IntPtr(GetWindowLong32(window, index));

    private static IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value) =>
        IntPtr.Size == 8 ? SetWindowLongPtr64(window, index, value) : new IntPtr(SetWindowLong32(window, index, value.ToInt32()));

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr window, int index, int value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr window, int index, IntPtr value);
}
