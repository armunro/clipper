using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Clipper.Core.Ports.Outbound;
using Clipper.Infrastructure.Adapters;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Clipper.Windows;

public partial class CaptureOverlayWindow : Window
{
    private readonly IScreenCaptureService _screenCaptureService;
    private Point _startPoint;
    private bool _isDragging;
    private Bitmap? _screenBitmap;
    private int _virtualLeft;
    private int _virtualTop;

    public BitmapSource? CapturedImage { get; private set; }
    public bool IsWindowCapture { get; private set; }

    public CaptureOverlayWindow(IScreenCaptureService screenCaptureService)
    {
        InitializeComponent();
        _screenCaptureService = screenCaptureService;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        CaptureFullScreen();
        Closed += (_, _) => DisposeScreenBitmap();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void CaptureFullScreen()
    {
        try
        {
            var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
            _virtualLeft = bounds.Left;
            _virtualTop = bounds.Top;
            _screenBitmap = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(_screenBitmap);
            g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }
        catch
        {
            _screenBitmap = null;
        }
    }

    private void DisposeScreenBitmap()
    {
        _screenBitmap?.Dispose();
        _screenBitmap = null;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _startPoint = e.GetPosition(this);
            _isDragging = false;
            InstructionText.Visibility = Visibility.Collapsed;
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point currentPoint = e.GetPosition(this);
            
            if (!_isDragging)
            {
                if (System.Math.Abs(currentPoint.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    System.Math.Abs(currentPoint.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    SelectionRect.Visibility = Visibility.Visible;
                }
            }

            if (_isDragging)
            {
                double x = System.Math.Min(_startPoint.X, currentPoint.X);
                double y = System.Math.Min(_startPoint.Y, currentPoint.Y);
                double width = System.Math.Abs(_startPoint.X - currentPoint.X);
                double height = System.Math.Abs(_startPoint.Y - currentPoint.Y);

                Canvas.SetLeft(SelectionRect, x);
                Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = width;
                SelectionRect.Height = height;
            }
        }
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        Point endPoint = e.GetPosition(this);

        if (_isDragging)
        {
            double x = System.Math.Min(_startPoint.X, endPoint.X);
            double y = System.Math.Min(_startPoint.Y, endPoint.Y);
            double width = System.Math.Abs(_startPoint.X - endPoint.X);
            double height = System.Math.Abs(_startPoint.Y - endPoint.Y);

            if (width > 5 && height > 5)
            {
                Point p1 = PointToScreen(new Point(x, y));
                Point p2 = PointToScreen(new Point(x + width, y + height));

                int screenX = (int)Math.Round(Math.Min(p1.X, p2.X));
                int screenY = (int)Math.Round(Math.Min(p1.Y, p2.Y));
                int screenWidth = (int)Math.Round(Math.Abs(p2.X - p1.X));
                int screenHeight = (int)Math.Round(Math.Abs(p2.Y - p1.Y));

                if (_screenBitmap != null && screenWidth > 0 && screenHeight > 0)
                {
                    int cropX = screenX - _virtualLeft;
                    int cropY = screenY - _virtualTop;

                    int srcX = Math.Max(0, Math.Min(cropX, _screenBitmap.Width - 1));
                    int srcY = Math.Max(0, Math.Min(cropY, _screenBitmap.Height - 1));
                    int srcW = Math.Min(screenWidth, _screenBitmap.Width - srcX);
                    int srcH = Math.Min(screenHeight, _screenBitmap.Height - srcY);

                    if (srcW > 0 && srcH > 0)
                    {
                        using Bitmap cropped = new Bitmap(srcW, srcH, _screenBitmap.PixelFormat);
                        using (Graphics g = Graphics.FromImage(cropped))
                        {
                            g.DrawImage(_screenBitmap, new Rectangle(0, 0, srcW, srcH), new Rectangle(srcX, srcY, srcW, srcH), GraphicsUnit.Pixel);
                        }
                        CapturedImage = WindowsScreenCaptureAdapter.ToBitmapSource(cropped);
                        DialogResult = true;
                    }
                }

                if (CapturedImage == null)
                {
                    this.Hide();
                    CapturedImage = _screenCaptureService.CaptureRegion(new Int32Rect(screenX, screenY, screenWidth, screenHeight));
                    DialogResult = true;
                }
            }
        }
        else
        {
            // Single click - capture window
            Point screenPoint = PointToScreen(endPoint);
            this.Hide();

            IntPtr hWnd = _screenCaptureService.GetWindowAt(screenPoint);
            if (hWnd != IntPtr.Zero)
            {
                CapturedImage = _screenCaptureService.CaptureWindow(hWnd);
                IsWindowCapture = true;
                DialogResult = true;
            }
        }
        
        if (DialogResult != true)
        {
            Close();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
