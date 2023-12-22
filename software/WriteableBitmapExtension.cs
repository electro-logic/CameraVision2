// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

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
        if (x < 0 || y < 0 || x > wb.PixelWidth - 1 || y > wb.PixelHeight - 1)
        {
            throw new ArgumentOutOfRangeException();
        }
        UInt16 b =0, g=0, r=0, a=0;
        wb.Lock();
        unsafe
        {
            UInt16* pBuff = (UInt16*)wb.BackBuffer.ToPointer();
            int stride = wb.BackBufferStride / 2;
            if (wb.Format == PixelFormats.Rgba64)
            {
                r = pBuff[4 * x + (y * stride)];
                g = pBuff[4 * x + (y * stride) + 1];
                b = pBuff[4 * x + (y * stride) + 2];
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        a = 0xFFFF;
        wb.Unlock();
        return Color.FromArgb(p16_to_p8(a), p16_to_p8(r), p16_to_p8(g), p16_to_p8(b));
    }

    static byte p16_to_p8(UInt16 v) => (byte)Math.Clamp(Math.Round(v / 255.0), 0, 255);

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
}