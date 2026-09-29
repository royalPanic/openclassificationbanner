using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Drawing;

namespace OpenClassBanner;

public sealed class AppBarHelper
{
    private const uint AbmNew = 0x00000000;
    private const uint AbmRemove = 0x00000001;
    private const uint AbmQueryPos = 0x00000002;
    private const uint AbmSetPos = 0x00000003;
    private const uint AbeTop = 1;
    private const int AbnPosChanged = 1;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoZOrder = 0x0004;
    private static readonly uint CallbackMessage = RegisterWindowMessage("OpenClassBanner.AppBar.Callback");

    private readonly IntPtr _windowHandle;
    private readonly HwndSource _source;
    private readonly Rectangle _monitorBounds;
    private readonly HwndSourceHook _hook;
    private bool _registered;
    private int _height;
    private bool _disposed;

    public AppBarHelper(IntPtr windowHandle, HwndSource source, Rectangle monitorBounds, int height)
    {
        _windowHandle = windowHandle;
        _source = source;
        _monitorBounds = monitorBounds;
        _height = height;
        _hook = WindowMessageHook;
        _source.AddHook(_hook);
    }

    public bool Register()
    {
        if (CallbackMessage == 0)
            return false;

        var data = CreateData();
        _registered = SHAppBarMessage(AbmNew, ref data) != IntPtr.Zero;
        Position();
        return _registered;
    }

    public void UpdateHeight(int height)
    {
        _height = height;
        Position();
    }

    private void Position()
    {
        var data = CreateData();
        if (_registered)
        {
            SHAppBarMessage(AbmQueryPos, ref data);
            var top = Math.Max(_monitorBounds.Top, data.Rect.Top);
            data.Rect = new NativeRect
            {
                Left = _monitorBounds.Left,
                Top = top,
                Right = _monitorBounds.Right,
                Bottom = Math.Min(_monitorBounds.Bottom, top + _height)
            };
            SHAppBarMessage(AbmSetPos, ref data);
        }
        else
        {
            data.Rect = new NativeRect
            {
                Left = _monitorBounds.Left,
                Top = _monitorBounds.Top,
                Right = _monitorBounds.Right,
                Bottom = Math.Min(_monitorBounds.Bottom, _monitorBounds.Top + _height)
            };
        }

        SetWindowPos(_windowHandle, IntPtr.Zero, data.Rect.Left, data.Rect.Top,
            data.Rect.Right - data.Rect.Left, data.Rect.Bottom - data.Rect.Top, SwpNoActivate | SwpNoZOrder);
    }

    private APPBARDATA CreateData() => new()
    {
        Size = (uint)Marshal.SizeOf<APPBARDATA>(),
        WindowHandle = _windowHandle,
        CallbackMessage = CallbackMessage,
        Edge = AbeTop,
        Rect = new NativeRect
        {
            Left = _monitorBounds.Left,
            Top = _monitorBounds.Top,
            Right = _monitorBounds.Right,
            Bottom = Math.Min(_monitorBounds.Bottom, _monitorBounds.Top + _height)
        }
    };

    private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == CallbackMessage && wParam.ToInt32() == AbnPosChanged)
        {
            Position();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _source.RemoveHook(_hook);
        if (_registered)
        {
            var data = CreateData();
            SHAppBarMessage(AbmRemove, ref data);
            _registered = false;
        }

        _disposed = true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public uint Size;
        public IntPtr WindowHandle;
        public uint CallbackMessage;
        public uint Edge;
        public NativeRect Rect;
        public IntPtr Parameter;
    }

    [DllImport("shell32.dll", EntryPoint = "SHAppBarMessage")]
    private static extern IntPtr SHAppBarMessage(uint message, ref APPBARDATA data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr windowHandle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
