using System.Windows.Input;

namespace Clipper;

public enum WindowPlacement
{
    BottomRight,
    Cursor,
    Center,
    BottomLeft,
    TopRight,
    TopLeft
}

public class HotkeyConfig
{
    public bool Control { get; set; } = true;
    public bool Shift { get; set; } = true;
    public bool Alt { get; set; } = false;
    public bool Win { get; set; } = false;
    public Key Key { get; set; } = Key.V;

    public uint GetModifiers()
    {
        uint mods = 0;
        if (Alt) mods |= 0x0001;
        if (Control) mods |= 0x0002;
        if (Shift) mods |= 0x0004;
        if (Win) mods |= 0x0008;
        return mods;
    }

    public uint GetVirtualKey()
    {
        return (uint)KeyInterop.VirtualKeyFromKey(Key);
    }

    public HotkeyConfig Clone()
    {
        return new HotkeyConfig
        {
            Control = this.Control,
            Shift = this.Shift,
            Alt = this.Alt,
            Win = this.Win,
            Key = this.Key
        };
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Control) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Win) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join(" + ", parts);
    }
}

public class AppSettings
{
    public WindowPlacement Placement { get; set; } = WindowPlacement.BottomRight;
    public HotkeyConfig Hotkey { get; set; } = new();

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Placement = this.Placement,
            Hotkey = this.Hotkey.Clone()
        };
    }
}
