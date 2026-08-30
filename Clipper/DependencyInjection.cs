using Clipper.Application.ActionHandlers;
using Clipper.Application.Services;
using Clipper.Core.Ports.Inbound;
using Clipper.Core.Ports.Outbound;
using Clipper.Infrastructure.Adapters;
using Clipper.Infrastructure.Persistence;
using Clipper.Infrastructure.UI;
using Clipper.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Clipper;

public static class DependencyInjection
{
    public static IServiceCollection AddClipperServices(this IServiceCollection services)
    {
        // Outbound Ports (Infrastructure Adapters)
        services.AddSingleton<ISettingsRepository, YamlSettingsRepository>();
        services.AddSingleton<IPatternRepository, YamlPatternRepository>();
        services.AddSingleton<ISystemClipboard, WindowsClipboardAdapter>();
        services.AddSingleton<IClipboardListener, WindowsClipboardListener>();
        services.AddSingleton<IHotkeyService, WindowsHotkeyAdapter>();
        services.AddSingleton<IScreenCaptureService, WindowsScreenCaptureAdapter>();
        services.AddSingleton<IUrlLauncher, ProcessUrlLauncher>();

        // Action Handlers (Polymorphic action strategies)
        services.AddSingleton<IClipboardActionHandler, OpenUrlActionHandler>();
        services.AddSingleton<IClipboardActionHandler, SearchWebActionHandler>();
        services.AddSingleton<IClipboardActionHandler, CopyActionHandler>();

        // Inbound Ports (Application Services)
        services.AddSingleton<IActionExecutor, ActionExecutor>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IPatternService, PatternService>();
        services.AddSingleton<IPatternProcessor, PatternProcessor>();
        services.AddSingleton<IClipboardHistoryService, ClipboardHistoryService>();
        services.AddSingleton<IWindowPlacementService, WindowPlacementService>();
        services.AddSingleton<IWindowService, WpfWindowService>();

        // Windows (Presentation)
        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<PatternEditorWindow>();
        services.AddTransient<CaptureOverlayWindow>();

        return services;
    }
}
