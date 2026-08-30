using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Clipper.Core.Ports.Outbound;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Clipper.Windows;

public partial class CaptureOverlayWindow : Window
{
    private readonly IScreenCaptureService _screenCaptureService;
    private Point _startPoint;
    private bool _isDragging;
    public BitmapSource? CapturedImage { get; private set; }
    public bool IsWindowCapture { get; private set; }

    public CaptureOverlayWindow(IScreenCaptureService screenCaptureService)
    {
        InitializeComponent();
        _screenCaptureService = screenCaptureService;
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
                // Convert screen coordinates BEFORE hiding
                Point screenPoint = PointToScreen(new Point(x, y));
                
                this.Opacity = 0;
                // Wait for opacity to apply
                System.Threading.Thread.Sleep(50);
                
                CapturedImage = _screenCaptureService.CaptureRegion(new Int32Rect((int)screenPoint.X, (int)screenPoint.Y, (int)width, (int)height));
                DialogResult = true;
            }
        }
        else
        {
            // Single click - capture window
            Point screenPoint = PointToScreen(endPoint);
            
            this.Opacity = 0;
            // Wait for opacity to apply
            System.Threading.Thread.Sleep(50);

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
