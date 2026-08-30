using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Clipper.Windows;

public partial class ImageEditorWindow : FluentWindow
{
    private System.Windows.Point _startPoint;
    private System.Windows.Shapes.Rectangle? _rectPreview;

    public BitmapSource? ResultImage { get; private set; }

    public ImageEditorWindow(BitmapSource sourceImage)
    {
        InitializeComponent();
        imgBase.Source = sourceImage;
        inkCanvas.Width = sourceImage.PixelWidth;
        inkCanvas.Height = sourceImage.PixelHeight;
        
        UpdateDrawingAttributes();
    }

    private void UpdateDrawingAttributes()
    {
        if (inkCanvas == null) return;

        var colorStr = (comboColor.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Red";
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorStr);

        var attributes = new DrawingAttributes
        {
            Color = color,
            Width = btnHighlighter.IsChecked == true ? 20 : 2,
            Height = btnHighlighter.IsChecked == true ? 20 : 2,
            IsHighlighter = btnHighlighter.IsChecked == true,
            FitToCurve = true
        };

        inkCanvas.DefaultDrawingAttributes = attributes;
    }

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (inkCanvas == null) return;

        if (btnEraser.IsChecked == true)
        {
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        }
        else if (btnRectOutlined.IsChecked == true || btnRectSolid.IsChecked == true || btnBlur.IsChecked == true)
        {
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
        }
        else
        {
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            UpdateDrawingAttributes();
        }
    }

    private void InkCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (btnRectOutlined.IsChecked != true && btnRectSolid.IsChecked != true && btnBlur.IsChecked != true) return;

        _startPoint = e.GetPosition(inkCanvas);

        var colorStr = (comboColor.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Red";
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorStr);

        if (btnBlur.IsChecked == true)
        {
            var visualBrush = new VisualBrush(imgBase)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None
            };

            var blurGrid = new Grid
            {
                IsHitTestVisible = false,
                ClipToBounds = true
            };

            var blurImage = new System.Windows.Shapes.Rectangle
            {
                Fill = visualBrush,
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 40, KernelType = System.Windows.Media.Effects.KernelType.Gaussian },
                Margin = new Thickness(-80) // Padding for blur
            };

            blurGrid.Children.Add(blurImage);
            _rectPreview = new System.Windows.Shapes.Rectangle
            {
                Fill = new VisualBrush(blurGrid),
                IsHitTestVisible = false
            };
        }
        else
        {
            _rectPreview = new System.Windows.Shapes.Rectangle
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };

            if (btnRectSolid.IsChecked == true)
            {
                _rectPreview.Fill = new SolidColorBrush(color);
            }
        }

        InkCanvas.SetLeft(_rectPreview, _startPoint.X);
        InkCanvas.SetTop(_rectPreview, _startPoint.Y);
        inkCanvas.Children.Add(_rectPreview);
    }

    private void InkCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_rectPreview == null) return;

        var currentPoint = e.GetPosition(inkCanvas);

        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(_startPoint.X - currentPoint.X);
        var height = Math.Abs(_startPoint.Y - currentPoint.Y);

        InkCanvas.SetLeft(_rectPreview, x);
        InkCanvas.SetTop(_rectPreview, y);
        _rectPreview.Width = width;
        _rectPreview.Height = height;

        if (_rectPreview.Fill is VisualBrush vb)
        {
            if (vb.Visual is Grid blurGrid && blurGrid.Children[0] is System.Windows.Shapes.Rectangle blurImage && blurImage.Fill is VisualBrush bvb)
            {
                bvb.Viewbox = new Rect(x, y, width, height);
                blurImage.Width = width + 160;
                blurImage.Height = height + 160;
            }
            else
            {
                vb.Viewbox = new Rect(x, y, width, height);
            }
        }
    }

    private void InkCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_rectPreview == null) return;

        // Convert the rectangle to a stroke to keep it in the same undo/redo stack if we had one, 
        // or just leave it as a child. Leaving it as a child is easier for rendering shapes.
        // However, for Clear_Click to work, we need to handle Children too.
        
        _rectPreview = null;
    }

    private void Color_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateDrawingAttributes();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        inkCanvas.Strokes.Clear();
        inkCanvas.Children.Clear();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (inkCanvas.Width <= 0 || inkCanvas.Height <= 0)
        {
            DialogResult = false;
            Close();
            return;
        }

        // Render the combined image
        var width = (int)inkCanvas.Width;
        var height = (int)inkCanvas.Height;

        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        
        // Use a Visual to draw both
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawImage(imgBase.Source, new Rect(0, 0, width, height));
        }
        rtb.Render(dv);
        rtb.Render(inkCanvas);

        ResultImage = rtb;
        DialogResult = true;
        Close();
    }
}
