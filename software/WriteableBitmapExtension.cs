// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraVision;

public static class WriteableBitmapExtension
{
    /// <summary>
    /// Save WriteableBitmap as PNG image
    /// </summary>
    public static void SavePNG(this WriteableBitmap wb, string filename)
    {
        using (FileStream stream = new FileStream(filename, FileMode.Create))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(wb));
            encoder.Save(stream);
        }
    }

    public static void SaveTiff(this WriteableBitmap wb, string filename, BitmapMetadata bmpMetadata = null, TiffCompressOption compression = TiffCompressOption.Zip)
    {
        using (var stream = new FileStream(filename, FileMode.Create))
        {
            var encoder = new TiffBitmapEncoder() { Compression = compression };
            var bmpFrame = BitmapFrame.Create(wb, null, bmpMetadata, null);
            encoder.Frames.Add(bmpFrame);
            encoder.Save(stream);
        }
    }

    public static Color GetPixel(this WriteableBitmap wb, int x, int y)
    {
        byte b, g, r, a;
        wb.Lock();
        unsafe
        {
            IntPtr pBackBuffer = wb.BackBuffer;
            byte* pBuff = (byte*)pBackBuffer.ToPointer();
            b = pBuff[4 * x + (y * wb.BackBufferStride)];
            g = pBuff[4 * x + (y * wb.BackBufferStride) + 1];
            r = pBuff[4 * x + (y * wb.BackBufferStride) + 2];
            if (wb.Format == PixelFormats.Bgra32)
            {
                a = pBuff[4 * x + (y * wb.BackBufferStride) + 3];
            }
            else
            {
                a = 0xFF;
            }
        }
        wb.Unlock();
        return Color.FromArgb(a, r, g, b);
    }
    public static void SetPixel(this WriteableBitmap wb, int x, int y, Color color)
    {
        wb.Lock();
        unsafe
        {
            IntPtr pBackBuffer = wb.BackBuffer;
            byte* pBuff = (byte*)pBackBuffer.ToPointer();
            pBuff[4 * x + (y * wb.BackBufferStride)] = color.B;
            pBuff[4 * x + (y * wb.BackBufferStride) + 1] = color.G;
            pBuff[4 * x + (y * wb.BackBufferStride) + 2] = color.R;
            if (wb.Format == PixelFormats.Bgra32)
            {
                pBuff[4 * x + (y * wb.BackBufferStride) + 3] = color.A;
            }
            else
            {
                pBuff[4 * x + (y * wb.BackBufferStride) + 3] = 0xFF;
            }
        }
        wb.AddDirtyRect(new System.Windows.Int32Rect(0, 0, (int)wb.Width, (int)wb.Height));
        wb.Unlock();
    }

    public static int GetBGRA32IntColor(this Color color)
    {
        return BitConverter.ToInt32(new byte[] { color.B, color.G, color.R, color.A }, 0);
    }
}