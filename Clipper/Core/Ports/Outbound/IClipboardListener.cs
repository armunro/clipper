namespace Clipper.Core.Ports.Outbound;

public interface IClipboardListener : IDisposable
{
    event EventHandler? ClipboardUpdated;
    void Register(IntPtr windowHandle);
    void Unregister(IntPtr windowHandle);
    bool HandleMessage(int msg);
    bool IsInternalCopy { get; set; }
}
