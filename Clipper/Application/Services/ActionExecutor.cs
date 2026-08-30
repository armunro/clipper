using Clipper.Application.ActionHandlers;
using Clipper.Core.Ports.Inbound;

namespace Clipper.Application.Services;

public class ActionExecutor : IActionExecutor
{
    private readonly Dictionary<ClipboardActionType, IClipboardActionHandler> _handlers;

    public ActionExecutor(IEnumerable<IClipboardActionHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.ActionType, h => h);
    }

    public void Execute(ClipboardAction action, string parameter)
    {
        if (_handlers.TryGetValue(action.Type, out var handler))
        {
            handler.Execute(action, parameter);
        }
    }

    public Action CreateActionCallback(ClipboardAction action, string parameter)
    {
        return () => Execute(action, parameter);
    }
}
