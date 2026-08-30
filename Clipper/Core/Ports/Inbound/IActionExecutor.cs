namespace Clipper.Core.Ports.Inbound;

public interface IActionExecutor
{
    void Execute(ClipboardAction action, string parameter);
    Action CreateActionCallback(ClipboardAction action, string parameter);
}
