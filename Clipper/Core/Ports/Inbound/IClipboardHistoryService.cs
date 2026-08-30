namespace Clipper.Core.Ports.Inbound;

public interface IClipboardHistoryService
{
    IReadOnlyList<ClipboardItem> History { get; }
    void AddItem(ClipboardItem item);
    void RemoveItem(ClipboardItem item);
    void Clear();
    IEnumerable<ClipboardItem> Search(string query);
    void ReprocessAll();
    event EventHandler<ClipboardItem>? ItemAdded;
    event EventHandler? HistoryUpdated;
}
