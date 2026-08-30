using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Clipper;

public enum ClipboardActionType
{
    OpenUrl,
    SearchWeb,
    Copy,
    None
}

public class ClipboardAction : INotifyPropertyChanged
{
    private string _label = string.Empty;
    private ClipboardActionType _type = ClipboardActionType.None;
    private string _parameterTemplate = string.Empty;

    public string Label
    {
        get => _label;
        set => SetField(ref _label, value);
    }

    public ClipboardActionType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    public string ParameterTemplate
    {
        get => _parameterTemplate;
        set => SetField(ref _parameterTemplate, value);
    }

    [JsonIgnore]
    public Action? Execute { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class ClipboardPattern : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _regexPattern = string.Empty;
    private List<ClipboardAction> _actions = new();

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string RegexPattern
    {
        get => _regexPattern;
        set => SetField(ref _regexPattern, value);
    }

    public List<ClipboardAction> Actions
    {
        get => _actions;
        set => SetField(ref _actions, value);
    }

    [JsonIgnore]
    public Func<Match, object?>? GetPreviewData { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
