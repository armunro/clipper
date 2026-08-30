using System.Windows;
using System.Windows.Media.Imaging;

namespace Clipper.Core.Ports.Outbound;

public interface IScreenCaptureService
{
    BitmapSource CaptureRegion(Int32Rect region);
    BitmapSource CaptureWindow(IntPtr hWnd);
    IntPtr GetWindowAt(System.Windows.Point point);
}
