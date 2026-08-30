using Clipper.Core.Ports.Outbound;

namespace Clipper.Application.ActionHandlers;

public class OpenUrlActionHandler : IClipboardActionHandler
{
    private readonly IUrlLauncher _urlLauncher;

    public OpenUrlActionHandler(IUrlLauncher urlLauncher)
    {
        _urlLauncher = urlLauncher;
    }

    public ClipboardActionType ActionType => ClipboardActionType.OpenUrl;

    public void Execute(ClipboardAction action, string parameter)
    {
        string targetUrl = string.IsNullOrWhiteSpace(action.ParameterTemplate)
            ? parameter
            : string.Format(action.ParameterTemplate, parameter);

        _urlLauncher.OpenUrl(targetUrl);
    }
}
