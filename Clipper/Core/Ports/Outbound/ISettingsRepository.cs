namespace Clipper.Core.Ports.Outbound;

public interface ISettingsRepository
{
    AppSettings Load();
    void Save(AppSettings settings);
}
