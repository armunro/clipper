namespace Clipper.Core.Ports.Inbound;

public interface IPatternProcessor
{
    void Process(ClipboardItem item);
}
