using System.Windows;
using System.Windows.Media;
using Clipper.Core.Ports.Inbound;

namespace Clipper.Application.Services;

public class WindowPlacementService : IWindowPlacementService
{
    public void ApplyPlacement(Window window, WindowPlacement placement)
    {
        var cursorPoint = System.Windows.Forms.Cursor.Position;
        var screen = System.Windows.Forms.Screen.FromPoint(cursorPoint) ?? System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null) return;

        var dpi = VisualTreeHelper.GetDpi(window);
        double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
        double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

        var workingArea = screen.WorkingArea;
        double workLeft = workingArea.Left / dpiX;
        double workTop = workingArea.Top / dpiY;
        double workWidth = workingArea.Width / dpiX;
        double workHeight = workingArea.Height / dpiY;
        double workRight = workLeft + workWidth;
        double workBottom = workTop + workHeight;

        switch (placement)
        {
            case WindowPlacement.BottomRight:
                window.Left = workRight - window.Width;
                window.Top = workBottom - window.Height;
                break;

            case WindowPlacement.BottomLeft:
                window.Left = workLeft;
                window.Top = workBottom - window.Height;
                break;

            case WindowPlacement.TopRight:
                window.Left = workRight - window.Width;
                window.Top = workTop;
                break;

            case WindowPlacement.TopLeft:
                window.Left = workLeft;
                window.Top = workTop;
                break;

            case WindowPlacement.Center:
                window.Left = workLeft + (workWidth - window.Width) / 2.0;
                window.Top = workTop + (workHeight - window.Height) / 2.0;
                break;

            case WindowPlacement.Cursor:
                double cursorX = cursorPoint.X / dpiX;
                double cursorY = cursorPoint.Y / dpiY;
                double targetLeft = cursorX - (window.Width / 2.0);
                double targetTop = cursorY - 30;

                if (targetLeft + window.Width > workRight) targetLeft = workRight - window.Width;
                if (targetLeft < workLeft) targetLeft = workLeft;
                if (targetTop + window.Height > workBottom) targetTop = workBottom - window.Height;
                if (targetTop < workTop) targetTop = workTop;

                window.Left = targetLeft;
                window.Top = targetTop;
                break;
        }
    }
}
