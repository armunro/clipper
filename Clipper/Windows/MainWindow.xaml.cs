using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using Clipper.Core.Ports.Inbound;
using Clipper.Core.Ports.Outbound;
using Wpf.Ui.Controls;

namespace Clipper.Windows;

public partial class MainWindow : FluentWindow
{
    private const int HOTKEY_LAUNCH_ID = 9000;
    private const int HOTKEY_CAPTURE_ID = 9001;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    private readonly ISystemClipboard _clipboard;
    private readonly IClipboardListener _clipboardListener;
    private readonly IClipboardHistoryService _historyService;
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IWindowPlacementService _windowPlacementService;
    private readonly IWindowService _windowService;

    private HwndSource? _hwndSource;
    private NotifyIcon? _notifyIcon;
    private bool _isExiting;
    private IntPtr _lastActiveWindow = IntPtr.Zero;

    public MainWindow(
        ISystemClipboard clipboard,
        IClipboardListener clipboardListener,
        IClipboardHistoryService historyService,
        ISettingsService settingsService,
        IHotkeyService hotkeyService,
        IWindowPlacementService windowPlacementService,
        IWindowService windowService)
    {
        InitializeComponent();
        ThemeHelper.ApplyDarkTheme(this);

        _clipboard = clipboard;
        _clipboardListener = clipboardListener;
        _historyService = historyService;
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _windowPlacementService = windowPlacementService;
        _windowService = windowService;

        _clipboardListener.ClipboardUpdated += OnClipboardUpdated;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _historyService.HistoryUpdated += (s, e) => UpdateHistoryList();

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

    private void RestoreWindow()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        IntPtr foreground = GetForegroundWindow();
        if (foreground != IntPtr.Zero && foreground != handle)
        {
            _lastActiveWindow = foreground;
        }

        ApplyWindowPlacement();
        this.Show();
        this.ShowInTaskbar = true;
        this.WindowState = WindowState.Normal;

        SetForegroundWindow(handle);
        this.Activate();
        this.Topmost = true;
        this.Topmost = false;
        this.Focus();

        if (this.Visibility != Visibility.Visible)
        {
            this.Visibility = Visibility.Visible;
        }

        txtSearch.Focus();
        if (lstHistory.Items.Count > 0 && lstHistory.SelectedIndex < 0)
        {
            lstHistory.SelectedIndex = 0;
        }
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

        _clipboardListener.Register(handle);
        RegisterLaunchHotkey();
        RegisterCaptureHotkey();
        ApplyWindowPlacement();
        UpdateHistoryList();
    }

    private void RegisterLaunchHotkey()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;

        _hotkeyService.UnregisterHotkey(handle, HOTKEY_LAUNCH_ID);

        var hotkey = _settingsService.Settings.Hotkey;
        uint modifiers = hotkey.GetModifiers();
        uint vk = hotkey.GetVirtualKey();

        bool registered = _hotkeyService.RegisterHotkey(handle, HOTKEY_LAUNCH_ID, modifiers, vk);
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

        _hotkeyService.UnregisterHotkey(handle, HOTKEY_CAPTURE_ID);
        _hotkeyService.RegisterHotkey(handle, HOTKEY_CAPTURE_ID, 0x0002 | 0x0004, 0x53); // Ctrl+Shift+S
    }

    private void ApplyWindowPlacement()
    {
        _windowPlacementService.ApplyPlacement(this, _settingsService.Settings.Placement);
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
        _clipboardListener.Unregister(handle);
        _hotkeyService.UnregisterAll(handle);
        _hwndSource?.RemoveHook(HwndHandler);
    }

    private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (_clipboardListener.HandleMessage(msg))
        {
            handled = true;
        }
        else if (_hotkeyService.HandleMessage(msg, wparam))
        {
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void OnHotkeyPressed(object? sender, int id)
    {
        if (id == HOTKEY_LAUNCH_ID)
        {
            IntPtr foreground = GetForegroundWindow();
            IntPtr handle = _hwndSource?.Handle ?? new WindowInteropHelper(this).Handle;
            if (foreground != IntPtr.Zero && foreground != handle)
            {
                _lastActiveWindow = foreground;
            }
            RestoreWindow();
        }
        else if (id == HOTKEY_CAPTURE_ID)
        {
            StartCapture();
        }
    }

    private void OnClipboardUpdated(object? sender, EventArgs e)
    {
        if (_clipboardListener.IsInternalCopy) return;

        ClipboardItem? newItem = null;
        try
        {
            if (_clipboard.ContainsText())
            {
                string? text = _clipboard.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    if (_historyService.History.FirstOrDefault()?.Text == text) return;

                    newItem = new ClipboardItem
                    {
                        Type = ClipboardItemType.Text,
                        Text = text
                    };
                }
            }
            else if (_clipboard.ContainsImage())
            {
                var image = _clipboard.GetImage();
                if (image != null)
                {
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
            System.Diagnostics.Debug.WriteLine($"Clipboard processing error: {ex.Message}");
        }

        if (newItem != null)
        {
            _historyService.AddItem(newItem);
            if (newItem.HasActions)
            {
                _windowService.ShowNotification(newItem);
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
        await Task.Delay(150);

        if (_windowService.ShowCaptureOverlayDialog(out var capturedImage, out _) == true && capturedImage != null)
        {
            var item = new ClipboardItem
            {
                Type = ClipboardItemType.Image,
                Image = capturedImage
            };

            _clipboard.SetClipboardItem(item);

            item.Actions.Add(new ClipboardAction
            {
                Label = "Markup & Edit",
                Type = ClipboardActionType.None,
                Execute = () => EditImage(item)
            });

            _historyService.AddItem(item);
            EditImage(item);
        }

        RestoreWindow();
    }

    private void EditImage(ClipboardItem item)
    {
        if (item.Image == null) return;

        if (_windowService.ShowImageEditorDialog(item.Image, out var editedImage, this) == true && editedImage != null)
        {
            if (editedImage.CanFreeze) editedImage.Freeze();

            var newItem = new ClipboardItem
            {
                Type = ClipboardItemType.Image,
                Image = editedImage
            };

            _clipboard.SetClipboardItem(newItem);

            newItem.Actions.Add(new ClipboardAction
            {
                Label = "Markup & Edit",
                Type = ClipboardActionType.None,
                Execute = () => EditImage(newItem)
            });

            _historyService.AddItem(newItem);
        }
    }

    private void UpdateHistoryList()
    {
        string filter = txtSearch.Text;
        lstHistory.ItemsSource = _historyService.Search(filter);
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateHistoryList();
    }

    private void ShowPatterns_Click(object sender, RoutedEventArgs e)
    {
        if (_windowService.ShowPatternEditorDialog(this) == true)
        {
            _historyService.ReprocessAll();
        }
    }

    private void ShowSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void ShowSettings()
    {
        if (_windowService.ShowSettingsDialog(this) == true)
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

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            this.Hide();
            this.ShowInTaskbar = false;
            e.Handled = true;
        }
    }

    private async void LstHistory_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            if (lstHistory.SelectedItem is ClipboardItem item)
            {
                e.Handled = true;
                await PasteItemAsync(item);
            }
        }
    }

    private async void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            e.Handled = true;
            if (lstHistory.SelectedItem is ClipboardItem selectedItem)
            {
                await PasteItemAsync(selectedItem);
            }
            else if (lstHistory.Items.Count > 0)
            {
                lstHistory.SelectedIndex = 0;
                if (lstHistory.SelectedItem is ClipboardItem firstItem)
                {
                    await PasteItemAsync(firstItem);
                }
            }
        }
        else if (e.Key == System.Windows.Input.Key.Down)
        {
            if (lstHistory.Items.Count > 0)
            {
                if (lstHistory.SelectedIndex < 0)
                {
                    lstHistory.SelectedIndex = 0;
                }
                lstHistory.Focus();
                if (lstHistory.ItemContainerGenerator.ContainerFromIndex(lstHistory.SelectedIndex) is System.Windows.Controls.ListBoxItem container)
                {
                    container.Focus();
                }
                e.Handled = true;
            }
        }
    }

    private async void LstHistory_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (lstHistory.SelectedItem is ClipboardItem item)
        {
            await PasteItemAsync(item);
        }
    }

    private async Task PasteItemAsync(ClipboardItem item)
    {
        _clipboardListener.IsInternalCopy = true;
        _clipboard.SetClipboardItem(item);

        this.Hide();
        this.ShowInTaskbar = false;

        IntPtr targetWindow = _lastActiveWindow;
        if (targetWindow != IntPtr.Zero && IsWindow(targetWindow))
        {
            SetForegroundWindow(targetWindow);
            await Task.Delay(60);
        }
        else
        {
            await Task.Delay(60);
        }

        SendPasteKeyStroke();
    }

    private static void SendPasteKeyStroke()
    {
        var inputs = new INPUT[4];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_CONTROL,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_V,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[2] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_V,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[3] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_CONTROL,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
