using System.Text.RegularExpressions;
using Clipper.Core.Ports.Inbound;

namespace Clipper.Application.Services;

public class PatternProcessor : IPatternProcessor
{
    private readonly IPatternService _patternService;
    private readonly IActionExecutor _actionExecutor;

    public PatternProcessor(IPatternService patternService, IActionExecutor actionExecutor)
    {
        _patternService = patternService;
        _actionExecutor = actionExecutor;
    }

    public void Process(ClipboardItem item)
    {
        if (item.Type != ClipboardItemType.Text || string.IsNullOrEmpty(item.Text)) return;

        string textToMatch = item.Text.Trim();

        foreach (var pattern in _patternService.Patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern.RegexPattern)) continue;

            Match match;
            try
            {
                match = Regex.Match(textToMatch, pattern.RegexPattern);
                if (!match.Success && textToMatch != item.Text)
                {
                    match = Regex.Match(item.Text, pattern.RegexPattern);
                }
            }
            catch
            {
                continue;
            }

            if (match.Success)
            {
                foreach (var actionTemplate in pattern.Actions)
                {
                    var action = new ClipboardAction
                    {
                        Label = actionTemplate.Label,
                        Type = actionTemplate.Type,
                        ParameterTemplate = actionTemplate.ParameterTemplate
                    };

                    string parameter = (match.Groups.Count == 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
                        ? match.Groups[1].Value
                        : match.Groups[0].Value;

                    action.Execute = _actionExecutor.CreateActionCallback(action, parameter);
                    item.Actions.Add(action);
                }

                if (item.PreviewData == null && pattern.Name == "Hex Color")
                {
                    item.PreviewData = match.Value;
                }
            }
        }
    }
}
