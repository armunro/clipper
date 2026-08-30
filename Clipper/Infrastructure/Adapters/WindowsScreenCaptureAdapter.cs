using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Clipper.Core.Ports.Outbound;
using Point = System.Windows.Point;

namespace Clipper.Infrastructure.Adapters;

public class WindowsScreenCaptureAdapter : IScreenCaptureService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public BitmapSource CaptureRegion(Int32Rect region)
    {
        using (Bitmap bitmap = new Bitmap(region.Width, region.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(region.X, region.Y, 0, 0, new System.Drawing.Size(region.Width, region.Height), CopyPixelOperation.SourceCopy);
            }
            return ToBitmapSource(bitmap);
        }
    }

    public BitmapSource CaptureWindow(IntPtr hWnd)
    {
        GetWindowRect(hWnd, out RECT rect);
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        using (Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
            }
            return ToBitmapSource(bitmap);
        }
    }

    public IntPtr GetWindowAt(Point point)
    {
        POINT p = new POINT { X = (int)point.X, Y = (int)point.Y };
        IntPtr hWnd = WindowFromPoint(p);

        IntPtr parent = GetParent(hWnd);
        while (parent != IntPtr.Zero)
        {
            hWnd = parent;
            parent = GetParent(hWnd);
        }

        return hWnd;
    }

    public static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
    }
}
