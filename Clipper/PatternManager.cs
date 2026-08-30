using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Clipper;

public class PatternManager
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Clipper",
        "patterns.yaml"
    );

    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public List<ClipboardPattern> Patterns { get; private set; } = new();

    public PatternManager()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        Load();
    }

    public void Load()
    {
        if (!File.Exists(ConfigPath))
        {
            Patterns = GetDefaultPatterns();
            Save();
            return;
        }

        try
        {
            string yaml = File.ReadAllText(ConfigPath);
            Patterns = _deserializer.Deserialize<List<ClipboardPattern>>(yaml) ?? GetDefaultPatterns();
            
            // Migration: Fix the URL pattern if it's the old broken one that used capturing groups
            var urlPattern = Patterns.FirstOrDefault(p => p.Name == "URL");
            if (urlPattern != null && (urlPattern.RegexPattern.Contains("(www\\.)") || urlPattern.RegexPattern.Contains("(?i)")))
            {
                var newDefaults = GetDefaultPatterns();
                var defaultUrl = newDefaults.First(p => p.Name == "URL");
                urlPattern.RegexPattern = defaultUrl.RegexPattern;
                Save();
            }

            // Migration: Ensure Hex Color has action so it can display notifications and preview actions
            var hexPattern = Patterns.FirstOrDefault(p => p.Name == "Hex Color");
            if (hexPattern != null && (hexPattern.Actions == null || hexPattern.Actions.Count == 0))
            {
                hexPattern.Actions = new List<ClipboardAction>
                {
                    new ClipboardAction { Label = "Copy Color", Type = ClipboardActionType.Copy, ParameterTemplate = "{0}" }
                };
                Save();
            }
        }
        catch
        {
            Patterns = GetDefaultPatterns();
        }
    }

    public void Save()
    {
        try
        {
            string directory = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            string yaml = _serializer.Serialize(Patterns);
            File.WriteAllText(ConfigPath, yaml);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save patterns: {ex.Message}");
        }
    }

    private List<ClipboardPattern> GetDefaultPatterns()
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
