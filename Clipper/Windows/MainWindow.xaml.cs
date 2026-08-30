using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Clipper;
using System.Windows.Forms;
using System.IO;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace Clipper.Windows;

public partial class MainWindow : FluentWindow
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;
    private const int HOTKEY_CAPTURE_ID = 9001;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private List<ClipboardItem> _history = new();
    private bool _isInternalCopy = false;
    private HwndSource? _hwndSource;
    private NotifyIcon? _notifyIcon;
    private bool _isExiting = false;
    private readonly PatternManager _patternManager;
    private readonly SettingsManager _settingsManager;

    public MainWindow()
    {
        InitializeComponent();
        _patternManager = new PatternManager();
        _settingsManager = new SettingsManager();
        ThemeHelper.ApplyDarkTheme(this);
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        SetupTrayIcon();
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new NotifyIcon();
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favicon.ico");
        if (File.Exists(iconPath))
        {
            _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
        }
        else
        {
            // If the icon is not in the base directory, try the project directory (for debugging)
            string projectIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "favicon.ico");
            if (File.Exists(projectIconPath))
            {
                _notifyIcon.Icon = new System.Drawing.Icon(projectIconPath);
            }
        }
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Clipper";
        _notifyIcon.DoubleClick += (s, e) => RestoreWindow();

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open", null, (s, e) => RestoreWindow());
        contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
    }


    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private void RestoreWindow()
    {
        ApplyWindowPlacement();
        this.Show();
        this.ShowInTaskbar = true;
        this.WindowState = WindowState.Normal;
        
        IntPtr handle = new WindowInteropHelper(this).Handle;
        SetForegroundWindow(handle);
        this.Activate();
        this.Topmost = true;
        this.Topmost = false;
        this.Focus();
        
        // Ensure we are not hidden if something else hid us
        if (this.Visibility != Visibility.Visible)
        {
            this.Visibility = Visibility.Visible;
        }

        // Focus the search box for immediate use
        txtSearch.Focus();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _notifyIcon?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(HwndHandler);
        AddClipboardFormatListener(handle);
        
        RegisterLaunchHotkey();
        RegisterCaptureHotkey();

        ApplyWindowPlacement();
    }

    private void RegisterLaunchHotkey()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;

        UnregisterHotKey(handle, HOTKEY_ID);

        var hotkey = _settingsManager.Settings.Hotkey;
        uint modifiers = hotkey.GetModifiers();
        uint vk = hotkey.GetVirtualKey();

        bool registered = RegisterHotKey(handle, HOTKEY_ID, modifiers, vk);
        if (!registered)
        {
            int error = Marshal.GetLastWin32Error();
            System.Windows.MessageBox.Show(
                $"Failed to register launch hotkey ({hotkey}). Error code: {error}.\nThe shortcut might already be in use by another application.",
                "Clipper Hotkey Warning",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    private void RegisterCaptureHotkey()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;

        UnregisterHotKey(handle, HOTKEY_CAPTURE_ID);
        RegisterHotKey(handle, HOTKEY_CAPTURE_ID, 0x0002 | 0x0004, 0x53); // Ctrl+Shift+S (MOD_CONTROL=0x0002, MOD_SHIFT=0x0004)
    }

    private void ApplyWindowPlacement()
    {
        var cursorPoint = System.Windows.Forms.Cursor.Position;
        var screen = System.Windows.Forms.Screen.FromPoint(cursorPoint) ?? System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null) return;

        var dpi = VisualTreeHelper.GetDpi(this);
        double dpiX = dpi.DpiScaleX;
        double dpiY = dpi.DpiScaleY;
        if (dpiX <= 0) dpiX = 1.0;
        if (dpiY <= 0) dpiY = 1.0;

        var workingArea = screen.WorkingArea;
        double workLeft = workingArea.Left / dpiX;
        double workTop = workingArea.Top / dpiY;
        double workWidth = workingArea.Width / dpiX;
        double workHeight = workingArea.Height / dpiY;
        double workRight = workLeft + workWidth;
        double workBottom = workTop + workHeight;

        switch (_settingsManager.Settings.Placement)
        {
            case WindowPlacement.BottomRight:
                this.Left = workRight - this.Width;
                this.Top = workBottom - this.Height;
                break;

            case WindowPlacement.BottomLeft:
                this.Left = workLeft;
                this.Top = workBottom - this.Height;
                break;

            case WindowPlacement.TopRight:
                this.Left = workRight - this.Width;
                this.Top = workTop;
                break;

            case WindowPlacement.TopLeft:
                this.Left = workLeft;
                this.Top = workTop;
                break;

            case WindowPlacement.Center:
                this.Left = workLeft + (workWidth - this.Width) / 2.0;
                this.Top = workTop + (workHeight - this.Height) / 2.0;
                break;

            case WindowPlacement.Cursor:
                double cursorX = cursorPoint.X / dpiX;
                double cursorY = cursorPoint.Y / dpiY;
                double targetLeft = cursorX - (this.Width / 2.0);
                double targetTop = cursorY - 30;

                if (targetLeft + this.Width > workRight) targetLeft = workRight - this.Width;
                if (targetLeft < workLeft) targetLeft = workLeft;
                if (targetTop + this.Height > workBottom) targetTop = workBottom - this.Height;
                if (targetTop < workTop) targetTop = workTop;

                this.Left = targetLeft;
                this.Top = targetTop;
                break;
        }
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e)
    {
        if (!_isExiting)
        {
            this.Hide();
            this.ShowInTaskbar = false;
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            this.Hide();
            this.ShowInTaskbar = false;
            return;
        }

        IntPtr handle = new WindowInteropHelper(this).Handle;
        RemoveClipboardFormatListener(handle);
        UnregisterHotKey(handle, HOTKEY_ID);
        UnregisterHotKey(handle, HOTKEY_CAPTURE_ID);
        _hwndSource?.RemoveHook(HwndHandler);
    }

    private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            if (!_isInternalCopy)
            {
                OnClipboardChanged();
            }
        }
        else if (msg == WM_HOTKEY)
        {
            int id = wparam.ToInt32();
            if (id == HOTKEY_ID)
            {
                RestoreWindow();
                handled = true;
            }
            else if (id == HOTKEY_CAPTURE_ID)
            {
                StartCapture();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        if (_isInternalCopy) return;
        ClipboardItem? newItem = null;

        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                string text = System.Windows.Clipboard.GetText();
                if (_history.FirstOrDefault()?.Text == text) return;

                newItem = new ClipboardItem 
                { 
                    Type = ClipboardItemType.Text, 
                    Text = text 
                };
            }
            else if (System.Windows.Clipboard.ContainsImage())
            {
                BitmapSource image = System.Windows.Clipboard.GetImage();
                if (image != null)
                {
                    // Freeze the image to avoid cross-thread issues and potential locks
                    if (image.CanFreeze) image.Freeze();
                    
                    newItem = new ClipboardItem 
                    { 
                        Type = ClipboardItemType.Image, 
                        Image = image 
                    };

                    newItem.Actions.Add(new ClipboardAction
                    {
                        Label = "Markup & Edit",
                        Type = ClipboardActionType.None,
                        Execute = () => EditImage(newItem)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // Clipboard access can be flakey
            System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}");
        }

        if (newItem != null)
        {
            ProcessItem(newItem);
            _history.Insert(0, newItem);
            if (_history.Count > 100) _history.RemoveAt(_history.Count - 1);
            UpdateHistoryList();

            if (newItem.HasActions)
            {
                var notification = new NotificationWindow(newItem);
                notification.Show();
            }
        }
    }

    private void Capture_Click(object sender, RoutedEventArgs e)
    {
        StartCapture();
    }

    private async void StartCapture()
    {
        this.Hide();
        // Small delay to let this window hide
        await System.Threading.Tasks.Task.Delay(150);

        var overlay = new CaptureOverlayWindow();
        bool? result = overlay.ShowDialog();
        
        if (result == true && overlay.CapturedImage != null)
        {
            var item = new ClipboardItem
            {
                Type = ClipboardItemType.Image,
                Image = overlay.CapturedImage
            };
            
            // Copy to clipboard immediately and handle _isInternalCopy
            SetClipboardItem(item);

            // Add to history immediately
            item.Actions.Add(new ClipboardAction
            {
                Label = "Markup & Edit",
                Type = ClipboardActionType.None,
                Execute = () => EditImage(item)
            });

            ProcessItem(item);
            _history.Insert(0, item);
            if (_history.Count > 100) _history.RemoveAt(_history.Count - 1);
            UpdateHistoryList();

            // Then open editor
            EditImage(item);
        }
        
        RestoreWindow();
    }

    private void EditImage(ClipboardItem item)
    {
        if (item.Image == null) return;

        var editor = new ImageEditorWindow(item.Image)
        {
            Owner = this
        };

        if (editor.ShowDialog() == true && editor.ResultImage != null)
        {
            var editedImage = editor.ResultImage;
            if (editedImage.CanFreeze) editedImage.Freeze();

            // Manually add to history
            var newItem = new ClipboardItem
            {
                Type = ClipboardItemType.Image,
                Image = editedImage
            };
            
            // Copy to clipboard and handle _isInternalCopy
            SetClipboardItem(newItem);

            newItem.Actions.Add(new ClipboardAction
            {
                Label = "Markup & Edit",
                Type = ClipboardActionType.None,
                Execute = () => EditImage(newItem)
            });

            ProcessItem(newItem);
            _history.Insert(0, newItem);
            if (_history.Count > 100) _history.RemoveAt(_history.Count - 1);
            UpdateHistoryList();
        }
    }

    private void SetClipboardItem(ClipboardItem item)
    {
        if (item == null) return;

        _isInternalCopy = true;
        try
        {
            var dataObject = new System.Windows.DataObject();
            if (item.Type == ClipboardItemType.Text && item.Text != null)
            {
                dataObject.SetText(item.Text);
            }
            else if (item.Type == ClipboardItemType.Image && item.Image != null)
            {
                dataObject.SetImage(item.Image);
            }

            // Retry loop because clipboard can be locked by other apps
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    System.Windows.Clipboard.SetDataObject(dataObject, true);
                    break;
                }
                catch (COMException)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}");
        }

        // Delay resetting _isInternalCopy to allow the system to process the update message
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (s, e) =>
        {
            _isInternalCopy = false;
            timer.Stop();
        };
        timer.Start();
    }

    private void ProcessItem(ClipboardItem item)
    {
        if (item.Type != ClipboardItemType.Text || string.IsNullOrEmpty(item.Text)) return;

        string textToMatch = item.Text.Trim();

        foreach (var pattern in _patternManager.Patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(textToMatch, pattern.RegexPattern);
            if (!match.Success && textToMatch != item.Text)
            {
                match = System.Text.RegularExpressions.Regex.Match(item.Text, pattern.RegexPattern);
            }
            if (match.Success)
            {
                foreach (var actionTemplate in pattern.Actions)
                {
                    var action = new ClipboardAction
                    {
                        Label = actionTemplate.Label,
                        Type = actionTemplate.Type,
                        ParameterTemplate = actionTemplate.ParameterTemplate
                    };

                    // If there is exactly one capturing group (in addition to the full match Group 0),
                    // and it's not empty, we assume that's the specific value the user wants to extract.
                    // Otherwise, we use the full match (Group 0) to avoid picking up incidental groups
                    // like (www\.) or other helper groups.
                    string parameter = (match.Groups.Count == 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
                        ? match.Groups[1].Value
                        : match.Groups[0].Value;
                    string finalValue = string.Format(action.ParameterTemplate, parameter);

                    action.Execute = action.Type switch
                    {
                        ClipboardActionType.OpenUrl => () => OpenUrl(finalValue),
                        ClipboardActionType.SearchWeb => () => OpenUrl($"https://www.google.com/search?q={Uri.EscapeDataString(finalValue)}"),
                        ClipboardActionType.Copy => () => {
                            _isInternalCopy = true;
                            System.Windows.Clipboard.SetText(finalValue);
                            _isInternalCopy = false;
                        },
                        _ => null
                    };

                    item.Actions.Add(action);
                }

                if (item.PreviewData == null && pattern.Name == "Hex Color")
                {
                    item.PreviewData = match.Value;
                }
            }
        }
    }

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Could not open URL: {ex.Message}");
        }
    }

    private void UpdateHistoryList()
    {
        string filter = txtSearch.Text.ToLower();
        var filteredItems = _history
            .Where(i => string.IsNullOrEmpty(filter) || 
                       (i.Text?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (i.Type == ClipboardItemType.Image && "[image]".Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(i => i.Timestamp)
            .ToList();

        lstHistory.ItemsSource = filteredItems;
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateHistoryList();
    }

    private void ShowPatterns_Click(object sender, RoutedEventArgs e)
    {
        var editor = new PatternEditorWindow(_patternManager);
        editor.Owner = this;
        if (editor.ShowDialog() == true)
        {
            // Re-process all items in history with new patterns
            foreach (var item in _history)
            {
                item.Actions.Clear();
                item.PreviewData = null;
                ProcessItem(item);
            }
            UpdateHistoryList();
        }
    }

    private void ShowSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void ShowSettings()
    {
        var settingsWindow = new SettingsWindow(_settingsManager);
        settingsWindow.Owner = this;
        if (settingsWindow.ShowDialog() == true)
        {
            RegisterLaunchHotkey();
            ApplyWindowPlacement();
        }
    }

    private void Action_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.DataContext is ClipboardAction action)
        {
            action.Execute?.Invoke();
        }
    }

    private void LstHistory_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (lstHistory.SelectedItem is ClipboardItem item)
        {
            PasteItem(item);
        }
    }

    private void PasteItem(ClipboardItem item)
    {
        SetClipboardItem(item);
        System.Windows.MessageBox.Show("Item copied back to clipboard. You can now paste it.");
    }
}