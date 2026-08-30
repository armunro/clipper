namespace Clipper.Core.Ports.Outbound;

public interface IPatternRepository
{
    List<ClipboardPattern> Load();
    void Save(IEnumerable<ClipboardPattern> patterns);
}
