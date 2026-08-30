using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Clipper.Core.Ports.Outbound;

namespace Clipper.Infrastructure.Adapters;

public class WindowsClipboardAdapter : ISystemClipboard
{
    public bool ContainsText()
    {
        try
        {
            return System.Windows.Clipboard.ContainsText();
        }
        catch
        {
            return false;
        }
    }

    public string? GetText()
    {
        try
        {
            return System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading text from clipboard: {ex.Message}");
            return null;
        }
    }

    public bool ContainsImage()
    {
        try
        {
            return System.Windows.Clipboard.ContainsImage();
        }
        catch
        {
            return false;
        }
    }

    public BitmapSource? GetImage()
    {
        try
        {
            if (System.Windows.Clipboard.ContainsImage())
            {
                var image = System.Windows.Clipboard.GetImage();
                if (image != null && image.CanFreeze)
                {
                    image.Freeze();
                }
                return image;
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading image from clipboard: {ex.Message}");
            return null;
        }
    }

    public void SetText(string text)
    {
        if (text == null) return;

        var dataObject = new System.Windows.DataObject();
        dataObject.SetText(text);
        SetDataObjectWithRetry(dataObject);
    }

    public void SetImage(BitmapSource image)
    {
        if (image == null) return;

        var dataObject = new System.Windows.DataObject();
        dataObject.SetImage(image);
        SetDataObjectWithRetry(dataObject);
    }

    public void SetClipboardItem(ClipboardItem item)
    {
        if (item == null) return;

        var dataObject = new System.Windows.DataObject();
        if (item.Type == ClipboardItemType.Text && item.Text != null)
        {
            dataObject.SetText(item.Text);
        }
        else if (item.Type == ClipboardItemType.Image && item.Image != null)
        {
            dataObject.SetImage(item.Image);
        }

        SetDataObjectWithRetry(dataObject);
    }

    private static void SetDataObjectWithRetry(System.Windows.DataObject dataObject)
    {
        for (int i = 0; i < 10; i++)
        {
            try
            {
                System.Windows.Clipboard.SetDataObject(dataObject, true);
                break;
            }
            catch (COMException)
            {
                System.Threading.Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting clipboard data: {ex.Message}");
                break;
            }
        }
    }
}
