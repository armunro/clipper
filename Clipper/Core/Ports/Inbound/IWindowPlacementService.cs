using System.Windows;

namespace Clipper.Core.Ports.Inbound;

public interface IWindowPlacementService
{
    void ApplyPlacement(Window window, WindowPlacement placement);
}
