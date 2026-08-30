using Clipper.Core.Ports.Inbound;
using Clipper.Core.Ports.Outbound;

namespace Clipper.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _settingsRepository;

    public AppSettings Settings { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    public SettingsService(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
        Load();
    }

    public void Load()
    {
        Settings = _settingsRepository.Load();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        _settingsRepository.Save(Settings);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSettings(AppSettings settings)
    {
        Settings = settings;
        Save();
    }
}
