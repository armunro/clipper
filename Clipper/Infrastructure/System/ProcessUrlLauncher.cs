using System.Diagnostics;
using System.Windows;
using Clipper.Core.Ports.Outbound;

namespace Clipper.Infrastructure.Adapters;

public class ProcessUrlLauncher : IUrlLauncher
{
    public void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not open URL '{url}': {ex.Message}");
            System.Windows.MessageBox.Show($"Could not open URL: {ex.Message}");
        }
    }
}
