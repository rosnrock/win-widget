using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace WinWidget.Services;

public sealed class TrayIconService : IDisposable
{
    private const int CallbackMessage = 0x8001;
    private const int WmRButtonUp = 0x0205;
    private const int WmLButtonDblClk = 0x0203;
    private const uint NimAdd = 0;
    private const uint NimDelete = 2;
    private const uint NifMessage = 1;
    private const uint NifIcon = 2;
    private const uint NifTip = 4;
    private const uint MfString = 0;
    private const uint TpmRightButton = 2;
    private readonly HwndSource _messageWindow;
    private readonly NotifyIconData _iconData;
    private bool _disposed;

    public event EventHandler? OpenRequested;
    public event EventHandler? ToggleLockRequested;
    public event EventHandler? ExitRequested;

    public TrayIconService(string tooltip = "WinWidget")
    {
        var parameters = new HwndSourceParameters("WinWidget.TrayMessageWindow")
        {
            Width = 0,
            Height = 0,
            WindowStyle = unchecked((int)0x80000000)
        };
        _messageWindow = new HwndSource(parameters);
        _messageWindow.AddHook(WindowProc);
        _iconData = new NotifyIconData
        {
            Size = Marshal.SizeOf<NotifyIconData>(),
            Window = _messageWindow.Handle,
            Id = 1,
            Flags = NifMessage | NifIcon | NifTip,
            CallbackMessage = CallbackMessage,
            Icon = LoadIcon(IntPtr.Zero, new IntPtr(32512)),
            Tip = tooltip
        };
        ShellNotifyIcon(NimAdd, ref _iconData);
    }

    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != CallbackMessage) return IntPtr.Zero;
        handled = true;
        if (lParam.ToInt32() == WmRButtonUp) ShowMenu(hwnd);
        else if (lParam.ToInt32() == WmLButtonDblClk) OpenRequested?.Invoke(this, EventArgs.Empty);
        return IntPtr.Zero;
    }

    private void ShowMenu(IntPtr owner)
    {
        var menu = CreatePopupMenu();
        try
        {
            AppendMenu(menu, MfString, 1, "Настройки");
            AppendMenu(menu, MfString, 2, "Закрепить / разблокировать");
            AppendMenu(menu, MfString, 3, "Выход");
            GetCursorPos(out var cursor);
            SetForegroundWindow(owner);
            var selected = TrackPopupMenu(menu, TpmRightButton | 0x0100, cursor.X, cursor.Y, 0, owner, IntPtr.Zero);
            if (selected == 1) OpenRequested?.Invoke(this, EventArgs.Empty);
            else if (selected == 2) ToggleLockRequested?.Invoke(this, EventArgs.Empty);
            else if (selected == 3) ExitRequested?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var data = _iconData;
        ShellNotifyIcon(NimDelete, ref data);
        _messageWindow.RemoveHook(WindowProc);
        _messageWindow.Dispose();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int Size;
        public IntPtr Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint TimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid Guid;
        public IntPtr BalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    private static bool ShellNotifyIcon(uint message, ref NotifyIconData data) => Shell_NotifyIcon(message, ref data);
    [DllImport("user32.dll")] private static extern IntPtr LoadIcon(IntPtr instance, IntPtr iconName);
    [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(IntPtr menu, uint flags, uint id, string text);
    [DllImport("user32.dll")] private static extern uint TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr owner, IntPtr rect);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
}
