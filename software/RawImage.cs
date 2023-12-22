using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraVision;

/// <summary>
/// RAW Bayer Image stored as 16 bit / pixel
/// </summary>
public partial class RawImage : ObservableObject
{
    [ObservableProperty]
    ushort[] _pixels;
    [ObservableProperty]
    int _width;
    [ObservableProperty]
    int _height;

    public RawImage(int width, int height)
    {
        _width = width;
        _height = height;
        _pixels = new ushort[_width * _height * sizeof(ushort)];
    }
    public ushort GetPixel(int x, int y)
    {
        if(x < 0 || y < 0 || x > Width-1 || y > Height-1)
        {
            throw new ArgumentOutOfRangeException();
        }
        return Pixels[x + y * Width];
    }
    public void SetPixel(int x, int y, ushort value) => Pixels[x + y * Width] = value;
    public RawImage Clone() => new RawImage(Width, Height) { Pixels = Pixels };

    public void SaveTiff(string filename, BitmapMetadata bmpMetadata = null, TiffCompressOption compression = TiffCompressOption.Zip)
    {
        using (var stream = new FileStream(filename, FileMode.Create))
        {
            var encoder = new TiffBitmapEncoder() { Compression = compression };
            var bmpSource = BitmapFrame.Create(Width, Height, 96, 96, PixelFormats.Gray16, null, Pixels, Width * 2);
            var bmpFrame = BitmapFrame.Create(bmpSource, null, bmpMetadata, null);
            encoder.Frames.Add(bmpFrame);
            encoder.Save(stream);
        }
    }
}