using System.IO;
using Clipper.Core.Ports.Outbound;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Clipper.Infrastructure.Persistence;

public class YamlPatternRepository : IPatternRepository
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Clipper",
        "patterns.yaml"
    );

    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public YamlPatternRepository()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public List<ClipboardPattern> Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaultPatterns = GetDefaultPatterns();
            Save(defaultPatterns);
            return defaultPatterns;
        }

        try
        {
            string yaml = File.ReadAllText(ConfigPath);
            var patterns = _deserializer.Deserialize<List<ClipboardPattern>>(yaml) ?? GetDefaultPatterns();

            // Migration: Fix the URL pattern if it's the old broken one that used capturing groups
            var urlPattern = patterns.FirstOrDefault(p => p.Name == "URL");
            if (urlPattern != null && (urlPattern.RegexPattern.Contains("(www\\.)") || urlPattern.RegexPattern.Contains("(?i)")))
            {
                var newDefaults = GetDefaultPatterns();
                var defaultUrl = newDefaults.First(p => p.Name == "URL");
                urlPattern.RegexPattern = defaultUrl.RegexPattern;
                Save(patterns);
            }

            // Migration: Ensure Hex Color has action so it can display notifications and preview actions
            var hexPattern = patterns.FirstOrDefault(p => p.Name == "Hex Color");
            if (hexPattern != null && (hexPattern.Actions == null || hexPattern.Actions.Count == 0))
            {
                hexPattern.Actions = new List<ClipboardAction>
                {
                    new ClipboardAction { Label = "Copy Color", Type = ClipboardActionType.Copy, ParameterTemplate = "{0}" }
                };
                Save(patterns);
            }

            return patterns;
        }
        catch
        {
            return GetDefaultPatterns();
        }
    }

    public void Save(IEnumerable<ClipboardPattern> patterns)
    {
        try
        {
            string directory = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            string yaml = _serializer.Serialize(patterns.ToList());
            File.WriteAllText(ConfigPath, yaml);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save patterns: {ex.Message}");
        }
    }

    private static List<ClipboardPattern> GetDefaultPatterns()
    {
        return new List<ClipboardPattern>
        {
            new ClipboardPattern
            {
                Name = "Jira Issue",
                RegexPattern = @"\b([A-Z]{2,}-[0-9]+)\b",
                Actions = new List<ClipboardAction>
                {
                    new ClipboardAction { Label = "Open in Jira", Type = ClipboardActionType.OpenUrl, ParameterTemplate = "https://yardi.atlassian.net/browse/{0}" }
                }
            },
            new ClipboardPattern
            {
                Name = "Email Address",
                RegexPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
                Actions = new List<ClipboardAction>
                {
                    new ClipboardAction { Label = "Email", Type = ClipboardActionType.OpenUrl, ParameterTemplate = "mailto:{0}" },
                    new ClipboardAction { Label = "Copy Email", Type = ClipboardActionType.Copy, ParameterTemplate = "{0}" }
                }
            },
            new ClipboardPattern
            {
                Name = "URL",
                RegexPattern = @"\bhttps?:\/\/(?:www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b(?:[-a-zA-Z0-9()@:%_\+.~#?&//=]*)",
                Actions = new List<ClipboardAction>
                {
                    new ClipboardAction { Label = "Open URL", Type = ClipboardActionType.OpenUrl, ParameterTemplate = "{0}" }
                }
            },
            new ClipboardPattern
            {
                Name = "IP Address",
                RegexPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b",
                Actions = new List<ClipboardAction>
                {
                    new ClipboardAction { Label = "Whois", Type = ClipboardActionType.OpenUrl, ParameterTemplate = "https://whois.domaintools.com/{0}" }
                }
            },
            new ClipboardPattern
            {
                Name = "GUID",
                RegexPattern = @"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b",
                Actions = new List<ClipboardAction>
                {
                    new ClipboardAction { Label = "Copy GUID", Type = ClipboardActionType.Copy, ParameterTemplate = "{0}" }
                }
            },
            new ClipboardPattern
            {
                Name = "Hex Color",
                RegexPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
                Actions = new List<ClipboardAction>
                {
                    new ClipboardAction { Label = "Copy Color", Type = ClipboardActionType.Copy, ParameterTemplate = "{0}" }
                }
            }
        };
    }
}
