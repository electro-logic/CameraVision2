// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System.IO;
using System.Windows.Media.Imaging;

namespace CameraVision;

public static class WriteableBitmapExtension
{
    /// <summary>
    /// Save WriteableBitmap as PNG image
    /// </summary>
    public static void SavePNG(this WriteableBitmap wb, string filename)
    {
        BitmapSource image = wb.Clone();
        using (FileStream stream = new FileStream(filename, FileMode.Create))
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);
        }
    }
}