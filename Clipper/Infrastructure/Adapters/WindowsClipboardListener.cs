using System.Runtime.InteropServices;
using System.Windows.Threading;
using Clipper.Core.Ports.Outbound;

namespace Clipper.Infrastructure.Adapters;

public class WindowsClipboardListener : IClipboardListener
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private IntPtr _registeredHandle = IntPtr.Zero;
    private bool _isInternalCopy;
    private DispatcherTimer? _internalResetTimer;

    public event EventHandler? ClipboardUpdated;

    public bool IsInternalCopy
    {
        get => _isInternalCopy;
        set
        {
            _isInternalCopy = value;
            if (value)
            {
                _internalResetTimer?.Stop();
                _internalResetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _internalResetTimer.Tick += (s, e) =>
                {
                    _isInternalCopy = false;
                    _internalResetTimer?.Stop();
                };
                _internalResetTimer.Start();
            }
        }
    }

    public void Register(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || _registeredHandle == windowHandle) return;

        Unregister(_registeredHandle);
        _registeredHandle = windowHandle;
        AddClipboardFormatListener(windowHandle);
    }

    public void Unregister(IntPtr windowHandle)
    {
        if (windowHandle != IntPtr.Zero)
        {
            RemoveClipboardFormatListener(windowHandle);
            if (_registeredHandle == windowHandle)
            {
                _registeredHandle = IntPtr.Zero;
            }
        }
    }

    public bool HandleMessage(int msg)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            if (!_isInternalCopy)
            {
                ClipboardUpdated?.Invoke(this, EventArgs.Empty);
            }
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (_registeredHandle != IntPtr.Zero)
        {
            Unregister(_registeredHandle);
        }
        _internalResetTimer?.Stop();
    }
}
