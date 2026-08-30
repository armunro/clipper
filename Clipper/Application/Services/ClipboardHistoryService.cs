using Clipper.Core.Ports.Inbound;

namespace Clipper.Application.Services;

public class ClipboardHistoryService : IClipboardHistoryService
{
    private const int MaxHistoryCount = 100;
    private readonly List<ClipboardItem> _history = new();
    private readonly object _lock = new();
    private readonly IPatternProcessor _patternProcessor;

    public event EventHandler<ClipboardItem>? ItemAdded;
    public event EventHandler? HistoryUpdated;

    public IReadOnlyList<ClipboardItem> History
    {
        get
        {
            lock (_lock)
            {
                return _history.ToList().AsReadOnly();
            }
        }
    }

    public ClipboardHistoryService(IPatternProcessor patternProcessor)
    {
        _patternProcessor = patternProcessor;
    }

    public void AddItem(ClipboardItem item)
    {
        lock (_lock)
        {
            _patternProcessor.Process(item);
            _history.Insert(0, item);
            if (_history.Count > MaxHistoryCount)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        ItemAdded?.Invoke(this, item);
        HistoryUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveItem(ClipboardItem item)
    {
        bool removed;
        lock (_lock)
        {
            removed = _history.Remove(item);
        }

        if (removed)
        {
            HistoryUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _history.Clear();
        }

        HistoryUpdated?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<ClipboardItem> Search(string query)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return _history.OrderByDescending(i => i.Timestamp).ToList();
            }

            string filter = query.Trim().ToLowerInvariant();
            return _history
                .Where(i => (i.Text?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (i.Type == ClipboardItemType.Image && "[image]".Contains(filter, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(i => i.Timestamp)
                .ToList();
        }
    }

    public void ReprocessAll()
    {
        lock (_lock)
        {
            foreach (var item in _history)
            {
                item.Actions.Clear();
                item.PreviewData = null;
                _patternProcessor.Process(item);
            }
        }

        HistoryUpdated?.Invoke(this, EventArgs.Empty);
    }
}
