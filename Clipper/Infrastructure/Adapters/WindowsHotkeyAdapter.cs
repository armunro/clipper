using System.Runtime.InteropServices;
using Clipper.Core.Ports.Outbound;

namespace Clipper.Infrastructure.Adapters;

public class WindowsHotkeyAdapter : IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HashSet<int> _registeredHotkeys = new();
    private IntPtr _registeredHandle = IntPtr.Zero;

    public event EventHandler<int>? HotkeyPressed;

    public bool RegisterHotkey(IntPtr windowHandle, int id, uint modifiers, uint vk)
    {
        if (windowHandle == IntPtr.Zero) return false;

        _registeredHandle = windowHandle;
        UnregisterHotkey(windowHandle, id);

        bool result = RegisterHotKey(windowHandle, id, modifiers, vk);
        if (result)
        {
            _registeredHotkeys.Add(id);
        }
        return result;
    }

    public void UnregisterHotkey(IntPtr windowHandle, int id)
    {
        if (windowHandle == IntPtr.Zero) return;

        UnregisterHotKey(windowHandle, id);
        _registeredHotkeys.Remove(id);
    }

    public void UnregisterAll(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero) return;

        foreach (var id in _registeredHotkeys.ToList())
        {
            UnregisterHotKey(windowHandle, id);
        }
        _registeredHotkeys.Clear();
    }

    public bool HandleMessage(int msg, IntPtr wParam)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            HotkeyPressed?.Invoke(this, id);
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (_registeredHandle != IntPtr.Zero)
        {
            UnregisterAll(_registeredHandle);
        }
    }
}
