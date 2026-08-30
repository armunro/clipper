using System.Windows;
using System.Windows.Media.Imaging;

namespace Clipper.Core.Ports.Inbound;

public interface IWindowService
{
    bool? ShowSettingsDialog(Window? owner = null);
    bool? ShowPatternEditorDialog(Window? owner = null);
    bool? ShowImageEditorDialog(BitmapSource image, out BitmapSource? resultImage, Window? owner = null);
    bool? ShowCaptureOverlayDialog(out BitmapSource? capturedImage, out bool isWindowCapture);
    void ShowNotification(ClipboardItem item);
}
