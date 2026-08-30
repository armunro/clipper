using Clipper.Core.Ports.Inbound;
using Clipper.Core.Ports.Outbound;

namespace Clipper.Application.Services;

public class PatternService : IPatternService
{
    private readonly IPatternRepository _patternRepository;
    private readonly List<ClipboardPattern> _patterns = new();

    public event EventHandler? PatternsChanged;

    public IReadOnlyList<ClipboardPattern> Patterns => _patterns.AsReadOnly();

    public PatternService(IPatternRepository patternRepository)
    {
        _patternRepository = patternRepository;
        Load();
    }

    public void Load()
    {
        _patterns.Clear();
        var loaded = _patternRepository.Load();
        _patterns.AddRange(loaded);
        PatternsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save(IEnumerable<ClipboardPattern> patterns)
    {
        _patterns.Clear();
        _patterns.AddRange(patterns);
        _patternRepository.Save(_patterns);
        PatternsChanged?.Invoke(this, EventArgs.Empty);
    }
}
