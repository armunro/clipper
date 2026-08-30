using System.Windows;
using System.Windows.Media.Imaging;
using Clipper.Core.Ports.Inbound;
using Clipper.Core.Ports.Outbound;
using Clipper.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Clipper.Infrastructure.UI;

public class WpfWindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WpfWindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool? ShowSettingsDialog(Window? owner = null)
    {
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        if (owner != null)
        {
            settingsWindow.Owner = owner;
        }
        return settingsWindow.ShowDialog();
    }

    public bool? ShowPatternEditorDialog(Window? owner = null)
    {
        var patternEditor = _serviceProvider.GetRequiredService<PatternEditorWindow>();
        if (owner != null)
        {
            patternEditor.Owner = owner;
        }
        return patternEditor.ShowDialog();
    }

    public bool? ShowImageEditorDialog(BitmapSource image, out BitmapSource? resultImage, Window? owner = null)
    {
        var editor = new ImageEditorWindow(image);
        if (owner != null)
        {
            editor.Owner = owner;
        }
        bool? dialogResult = editor.ShowDialog();
        resultImage = editor.ResultImage;
        return dialogResult;
    }

    public bool? ShowCaptureOverlayDialog(out BitmapSource? capturedImage, out bool isWindowCapture)
    {
        var screenCaptureService = _serviceProvider.GetRequiredService<IScreenCaptureService>();
        var overlay = new CaptureOverlayWindow(screenCaptureService);
        bool? dialogResult = overlay.ShowDialog();
        capturedImage = overlay.CapturedImage;
        isWindowCapture = overlay.IsWindowCapture;
        return dialogResult;
    }

    public void ShowNotification(ClipboardItem item)
    {
        var notification = new NotificationWindow(item);
        notification.Show();
    }
}
