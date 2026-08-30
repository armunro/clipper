using Clipper.Core.Ports.Outbound;

namespace Clipper.Application.ActionHandlers;

public class SearchWebActionHandler : IClipboardActionHandler
{
    private readonly IUrlLauncher _urlLauncher;

    public SearchWebActionHandler(IUrlLauncher urlLauncher)
    {
        _urlLauncher = urlLauncher;
    }

    public ClipboardActionType ActionType => ClipboardActionType.SearchWeb;

    public void Execute(ClipboardAction action, string parameter)
    {
        string term = string.IsNullOrWhiteSpace(action.ParameterTemplate)
            ? parameter
            : string.Format(action.ParameterTemplate, parameter);

        string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(term)}";
        _urlLauncher.OpenUrl(searchUrl);
    }
}
