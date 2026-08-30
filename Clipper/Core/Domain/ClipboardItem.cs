using System.Windows.Media.Imaging;

namespace Clipper;

public enum ClipboardItemType
{
    Text,
    Image
}

public class ClipboardItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ClipboardItemType Type { get; set; }
    public string? Text { get; set; }
    public BitmapSource? Image { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public List<ClipboardAction> Actions { get; set; } = new();
    public object? PreviewData { get; set; }

    public bool HasActions => Actions.Count > 0;
    public bool HasColorPreview => PreviewData is string s && s.StartsWith("#");
    public string? ColorPreview => HasColorPreview ? (string)PreviewData! : null;
    public bool HasImage => Image != null;

    public string DisplayText => Type == ClipboardItemType.Text 
        ? (Text?.Length > 500 ? Text[..497] + "..." : (Text?.Trim() ?? "")) 
        : "[Image]";
}
