using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipper.Windows;

public partial class NotificationWindow : Window
{
    private readonly DispatcherTimer _closeTimer;

    public NotificationWindow(ClipboardItem item)
    {
        InitializeComponent();
        
        txtContent.Text = item.DisplayText;
        itemsActions.ItemsSource = item.Actions;

        if (item.HasColorPreview && !string.IsNullOrEmpty(item.ColorPreview))
        {
            try
            {
                var brush = new System.Windows.Media.BrushConverter().ConvertFromString(item.ColorPreview) as System.Windows.Media.Brush;
                if (brush != null)
                {
                    rectColorPreview.Fill = brush;
                    rectColorPreview.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                rectColorPreview.Visibility = Visibility.Collapsed;
            }
        }
        else if (item.HasImage && item.Image != null)
        {
            imgPreview.Source = item.Image;
            borderImagePreview.Visibility = Visibility.Visible;
        }

        _closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(8)
        };
        _closeTimer.Tick += (s, e) => Close();
        _closeTimer.Start();

        Loaded += NotificationWindow_Loaded;
    }

    private void NotificationWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var desktopWorkingArea = SystemParameters.WorkArea;
        Left = desktopWorkingArea.Right - Width;
        Top = desktopWorkingArea.Bottom - Height;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Action_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ClipboardAction action)
        {
            action.Execute?.Invoke();
            Close();
        }
    }
}
