using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Clipper;

public class SettingsManager
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Clipper",
        "settings.yaml"
    );

    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public AppSettings Settings { get; private set; } = new();

    public SettingsManager()
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
            Settings = new AppSettings();
            Save();
            return;
        }

        try
        {
            string yaml = File.ReadAllText(ConfigPath);
            Settings = _deserializer.Deserialize<AppSettings>(yaml) ?? new AppSettings();
        }
        catch
        {
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            string directory = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            string yaml = _serializer.Serialize(Settings);
            File.WriteAllText(ConfigPath, yaml);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}
