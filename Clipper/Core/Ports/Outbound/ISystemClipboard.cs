using System.Windows.Media.Imaging;

namespace Clipper.Core.Ports.Outbound;

public interface ISystemClipboard
{
    bool ContainsText();
    string? GetText();
    bool ContainsImage();
    BitmapSource? GetImage();
    void SetText(string text);
    void SetImage(BitmapSource image);
    void SetClipboardItem(ClipboardItem item);
}
