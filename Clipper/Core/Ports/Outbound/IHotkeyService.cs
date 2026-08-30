namespace Clipper.Core.Ports.Outbound;

public interface IHotkeyService : IDisposable
{
    event EventHandler<int>? HotkeyPressed;
    bool RegisterHotkey(IntPtr windowHandle, int id, uint modifiers, uint vk);
    void UnregisterHotkey(IntPtr windowHandle, int id);
    void UnregisterAll(IntPtr windowHandle);
    bool HandleMessage(int msg, IntPtr wParam);
}
