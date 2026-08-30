namespace Clipper.Application.ActionHandlers;

public interface IClipboardActionHandler
{
    ClipboardActionType ActionType { get; }
    void Execute(ClipboardAction action, string parameter);
}
