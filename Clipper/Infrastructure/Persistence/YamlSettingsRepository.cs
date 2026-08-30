using System.IO;
using Clipper.Core.Ports.Outbound;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Clipper.Infrastructure.Persistence;

public class YamlSettingsRepository : ISettingsRepository
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Clipper",
        "settings.yaml"
    );

    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public YamlSettingsRepository()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public AppSettings Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }

        try
        {
            string yaml = File.ReadAllText(ConfigPath);
            return _deserializer.Deserialize<AppSettings>(yaml) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            string directory = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            string yaml = _serializer.Serialize(settings);
            File.WriteAllText(ConfigPath, yaml);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}
