namespace Clipper.Core.Ports.Inbound;

public interface IPatternService
{
    IReadOnlyList<ClipboardPattern> Patterns { get; }
    void Load();
    void Save(IEnumerable<ClipboardPattern> patterns);
    event EventHandler? PatternsChanged;
}
