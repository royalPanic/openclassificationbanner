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
    private static readonly (uint Message, int Error) CallbackRegistration = RegisterCallbackMessage();
    private static uint CallbackMessage => CallbackRegistration.Message;

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
        {
            Logger.Debug($"RegisterWindowMessage failed; win32Error={CallbackRegistration.Error}.");
            return false;
        }

        var data = CreateData();
        var result = SHAppBarMessage(AbmNew, ref data);
        _registered = result != IntPtr.Zero;
        Logger.Debug($"ABM_NEW result=0x{result.ToInt64():X}; registered={_registered}; callbackMessage=0x{CallbackMessage:X}; monitorBounds={_monitorBounds}; height={_height}px.");
        Position();
        return _registered;
    }

    public void UpdateHeight(int height)
    {
        Logger.Debug($"Updating AppBar height from {_height}px to {height}px for monitor bounds {_monitorBounds}.");
        _height = height;
        Position();
    }

    private void Position()
    {
        var data = CreateData();
        if (_registered)
        {
            var queryResult = SHAppBarMessage(AbmQueryPos, ref data);
            Logger.Debug($"ABM_QUERYPOS result=0x{queryResult.ToInt64():X}; returnedRect=({data.Rect.Left},{data.Rect.Top},{data.Rect.Right},{data.Rect.Bottom}).");
            var top = Math.Max(_monitorBounds.Top, data.Rect.Top);
            data.Rect = new NativeRect
            {
                Left = _monitorBounds.Left,
                Top = top,
                Right = _monitorBounds.Right,
                Bottom = Math.Min(_monitorBounds.Bottom, top + _height)
            };
            var setResult = SHAppBarMessage(AbmSetPos, ref data);
            Logger.Debug($"ABM_SETPOS result=0x{setResult.ToInt64():X}; requestedRect=({data.Rect.Left},{data.Rect.Top},{data.Rect.Right},{data.Rect.Bottom}).");
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

        var positioned = SetWindowPos(_windowHandle, IntPtr.Zero, data.Rect.Left, data.Rect.Top,
            data.Rect.Right - data.Rect.Left, data.Rect.Bottom - data.Rect.Top, SwpNoActivate | SwpNoZOrder);
        Logger.Debug($"SetWindowPos success={positioned}; win32Error={(positioned ? 0 : Marshal.GetLastWin32Error())}; rect=({data.Rect.Left},{data.Rect.Top},{data.Rect.Right},{data.Rect.Bottom}); registered={_registered}.");
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
            var result = SHAppBarMessage(AbmRemove, ref data);
            Logger.Debug($"ABM_REMOVE result=0x{result.ToInt64():X}; monitorBounds={_monitorBounds}.");
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

    private static (uint Message, int Error) RegisterCallbackMessage()
    {
        var message = RegisterWindowMessage("OpenClassBanner.AppBar.Callback");
        return (message, Marshal.GetLastWin32Error());
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr windowHandle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
