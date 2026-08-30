using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Clipper.Windows;

public partial class SettingsWindow : FluentWindow
{
    private readonly SettingsManager _settingsManager;
    private HotkeyConfig _currentHotkey;
    private bool _isUpdatingUi = false;
    private bool _isRecording = false;

    public SettingsWindow(SettingsManager settingsManager)
    {
        InitializeComponent();
        _settingsManager = settingsManager;
        ThemeHelper.ApplyDarkTheme(this);

        _currentHotkey = _settingsManager.Settings.Hotkey.Clone();

        PopulateKeys();
        LoadSettingsToUi();
    }

    private void PopulateKeys()
    {
        var keys = new List<Key>();

        // Letters A-Z
        for (Key k = Key.A; k <= Key.Z; k++)
        {
            keys.Add(k);
        }

        // Numbers 0-9
        for (Key k = Key.D0; k <= Key.D9; k++)
        {
            keys.Add(k);
        }

        // Function keys F1-F12
        for (Key k = Key.F1; k <= Key.F12; k++)
        {
            keys.Add(k);
        }

        // Additional common keys
        keys.AddRange(new[]
        {
            Key.Space, Key.Insert, Key.Delete, Key.Home, Key.End,
            Key.PageUp, Key.PageDown, Key.Tab, Key.Back,
            Key.OemTilde, Key.OemMinus, Key.OemPlus, Key.OemOpenBrackets,
            Key.OemCloseBrackets, Key.Oem1, Key.Oem2, Key.Oem3, Key.Oem5,
            Key.Oem6, Key.Oem7, Key.OemPeriod, Key.OemComma
        });

        cmbKey.ItemsSource = keys;
    }

    private void LoadSettingsToUi()
    {
        _isUpdatingUi = true;

        // Window placement
        foreach (ComboBoxItem item in cmbPlacement.Items)
        {
            if (item.Tag is WindowPlacement placement && placement == _settingsManager.Settings.Placement)
            {
                cmbPlacement.SelectedItem = item;
                break;
            }
        }
        if (cmbPlacement.SelectedItem == null)
        {
            cmbPlacement.SelectedIndex = 0;
        }

        // Hotkey
        chkCtrl.IsChecked = _currentHotkey.Control;
        chkShift.IsChecked = _currentHotkey.Shift;
        chkAlt.IsChecked = _currentHotkey.Alt;
        chkWin.IsChecked = _currentHotkey.Win;
        cmbKey.SelectedItem = _currentHotkey.Key;

        UpdateHotkeyDisplay();

        _isUpdatingUi = false;
    }

    private void UpdateHotkeyDisplay()
    {
        txtHotkeyDisplay.Text = _isRecording ? "Press keys on keyboard..." : _currentHotkey.ToString();
    }

    private void TxtHotkeyDisplay_GotFocus(object sender, RoutedEventArgs e)
    {
        _isRecording = true;
        UpdateHotkeyDisplay();
    }

    private void TxtHotkeyDisplay_LostFocus(object sender, RoutedEventArgs e)
    {
        _isRecording = false;
        UpdateHotkeyDisplay();
    }

    private void TxtHotkeyDisplay_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

        // Ignore standalone modifier key presses
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        bool win = (Keyboard.Modifiers & ModifierKeys.Windows) != 0;

        // Require at least one modifier or F1-F12 to avoid capturing standard keys globally by accident
        if (!ctrl && !alt && !shift && !win && !(key >= Key.F1 && key <= Key.F12))
        {
            ctrl = true;
        }

        _currentHotkey.Control = ctrl;
        _currentHotkey.Alt = alt;
        _currentHotkey.Shift = shift;
        _currentHotkey.Win = win;
        _currentHotkey.Key = key;

        _isUpdatingUi = true;
        chkCtrl.IsChecked = ctrl;
        chkAlt.IsChecked = alt;
        chkShift.IsChecked = shift;
        chkWin.IsChecked = win;
        cmbKey.SelectedItem = key;
        _isUpdatingUi = false;

        _isRecording = false;
        UpdateHotkeyDisplay();
        e.Handled = true;
    }

    private void Modifier_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUi) return;

        _currentHotkey.Control = chkCtrl.IsChecked == true;
        _currentHotkey.Shift = chkShift.IsChecked == true;
        _currentHotkey.Alt = chkAlt.IsChecked == true;
        _currentHotkey.Win = chkWin.IsChecked == true;

        UpdateHotkeyDisplay();
    }

    private void CmbKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi) return;

        if (cmbKey.SelectedItem is Key key)
        {
            _currentHotkey.Key = key;
            UpdateHotkeyDisplay();
        }
    }

    private void ResetHotkey_Click(object sender, RoutedEventArgs e)
    {
        _currentHotkey = new HotkeyConfig();
        LoadSettingsToUi();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (cmbPlacement.SelectedItem is ComboBoxItem selectedPlacement && selectedPlacement.Tag is WindowPlacement placement)
        {
            _settingsManager.Settings.Placement = placement;
        }

        _settingsManager.Settings.Hotkey = _currentHotkey.Clone();
        _settingsManager.Save();

        DialogResult = true;
        Close();
    }
}
