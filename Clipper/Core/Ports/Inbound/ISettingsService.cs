namespace Clipper.Core.Ports.Inbound;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void Load();
    void Save();
    void UpdateSettings(AppSettings settings);
    event EventHandler? SettingsChanged;
}
