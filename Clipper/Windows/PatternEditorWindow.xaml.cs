using System.Windows;
using System.Windows.Controls;
using Clipper;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace Clipper.Windows;

public partial class PatternEditorWindow : FluentWindow
{
    private readonly PatternManager _manager;
    private ObservableCollection<ClipboardPattern> _patterns;
    private ClipboardPattern? _selectedPattern;

    public PatternEditorWindow(PatternManager manager)
    {
        InitializeComponent();
        ThemeHelper.ApplyDarkTheme(this);
        _manager = manager;
        _patterns = new ObservableCollection<ClipboardPattern>(_manager.Patterns);
        lstPatterns.ItemsSource = _patterns;
    }

    private bool _isUpdating = false;

    private void LstPatterns_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating) return;
        
        SaveCurrentPattern();
        
        _isUpdating = true;
        try
        {
            _selectedPattern = lstPatterns.SelectedItem as ClipboardPattern;
            
            if (_selectedPattern != null)
            {
                pnlDetails.Visibility = Visibility.Visible;
                txtName.Text = _selectedPattern.Name;
                txtRegex.Text = _selectedPattern.RegexPattern;
                itemsActions.ItemsSource = new ObservableCollection<ClipboardAction>(_selectedPattern.Actions);
            }
            else
            {
                pnlDetails.Visibility = Visibility.Collapsed;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SaveCurrentPattern()
    {
        if (_selectedPattern != null)
        {
            _selectedPattern.Name = txtName.Text;
            _selectedPattern.RegexPattern = txtRegex.Text;
            _selectedPattern.Actions = (itemsActions.ItemsSource as ObservableCollection<ClipboardAction>)?.ToList() ?? new List<ClipboardAction>();
        }
    }

    private void AddPattern_Click(object sender, RoutedEventArgs e)
    {
        var newPattern = new ClipboardPattern { Name = "New Pattern", RegexPattern = ".*" };
        _isUpdating = true;
        try
        {
            _patterns.Add(newPattern);
            lstPatterns.SelectedItem = newPattern;
            _selectedPattern = newPattern;
            
            pnlDetails.Visibility = Visibility.Visible;
            txtName.Text = newPattern.Name;
            txtRegex.Text = newPattern.RegexPattern;
            itemsActions.ItemsSource = new ObservableCollection<ClipboardAction>(newPattern.Actions);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void DeletePattern_Click(object sender, RoutedEventArgs e)
    {
        if (lstPatterns.SelectedItem is ClipboardPattern pattern)
        {
            _isUpdating = true;
            try
            {
                _patterns.Remove(pattern);
                _selectedPattern = null;
                pnlDetails.Visibility = Visibility.Collapsed;
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    private void AddAction_Click(object sender, RoutedEventArgs e)
    {
        if (itemsActions.ItemsSource is ObservableCollection<ClipboardAction> actions)
        {
            actions.Add(new ClipboardAction { Label = "New Action", Type = ClipboardActionType.OpenUrl });
        }
    }

    private void RemoveAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.DataContext is ClipboardAction action)
        {
            if (itemsActions.ItemsSource is ObservableCollection<ClipboardAction> actions)
            {
                actions.Remove(action);
            }
        }
    }

    private void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentPattern();
        _manager.Patterns.Clear();
        _manager.Patterns.AddRange(_patterns);
        _manager.Save();
        DialogResult = true;
        Close();
    }
}
