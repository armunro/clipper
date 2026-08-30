using Clipper.Core.Ports.Outbound;

namespace Clipper.Application.ActionHandlers;

public class CopyActionHandler : IClipboardActionHandler
{
    private readonly ISystemClipboard _clipboard;
    private readonly IClipboardListener _clipboardListener;

    public CopyActionHandler(ISystemClipboard clipboard, IClipboardListener clipboardListener)
    {
        _clipboard = clipboard;
        _clipboardListener = clipboardListener;
    }

    public ClipboardActionType ActionType => ClipboardActionType.Copy;

    public void Execute(ClipboardAction action, string parameter)
    {
        string text = string.IsNullOrWhiteSpace(action.ParameterTemplate)
            ? parameter
            : string.Format(action.ParameterTemplate, parameter);

        _clipboardListener.IsInternalCopy = true;
        try
        {
            _clipboard.SetText(text);
        }
        finally
        {
            // Reset is handled or listener handles internal flag
            _clipboardListener.IsInternalCopy = false;
        }
    }
}
