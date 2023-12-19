// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Windows.Media.Imaging;

namespace CameraVision;

public static class Metadata
{
    public static BitmapMetadata GetTIFFMetadata(string maker, string model, double f, double fstop, double sensorWidth, double sensorHeight, double exposureMs, UInt16 iso)
    {
        double sensor35Diagonal = Math.Sqrt(36 * 36 + 24 * 24);
        double sensorDiagonal = Math.Sqrt(sensorWidth * sensorWidth + sensorHeight * sensorHeight);
        double cropFactor = sensor35Diagonal / sensorDiagonal;
        double focalLengthIn35mmFilm = cropFactor * f;
        double exposureSecs = exposureMs / 1000.0;
        // Metadata query documentation
        // https://learn.microsoft.com/en-us/windows/win32/wic/system-photo
        // https://www.awaresystems.be/imaging/tiff.html
        // https://exiftool.org/TagNames/EXIF.html
        var bmpMetadata = new BitmapMetadata("tiff");
        // TIFF Exif metadata
        bmpMetadata.SetQuery("/ifd/exif/{ushort=37386}", ExifRational(f));
        bmpMetadata.SetQuery("/ifd/exif/{ushort=41989}", (ushort)focalLengthIn35mmFilm);
        bmpMetadata.SetQuery("/ifd/exif/{ushort=33437}", ExifRational(fstop));
        bmpMetadata.SetQuery("/ifd/exif/{ushort=33434}", ExifRational(exposureSecs));                   // Exposure time (seconds)
        bmpMetadata.SetQuery("/ifd/exif/{ushort=34855}", iso);
        bmpMetadata.SetQuery("/ifd/exif/{ushort=41987}", (ushort)1);                                    // White Balance (0 = Auto white balance, 1 = Manual white balance)
        bmpMetadata.SetQuery("/ifd/exif/{ushort=36867}", DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss")); // Date taken
        bmpMetadata.SetQuery("/ifd/{ushort=271}", maker);
        bmpMetadata.SetQuery("/ifd/{ushort=272}", model);
        // TIFF XMP metadata
        bmpMetadata.SetQuery("/ifd/xmp/exif:FocalLength", f.ToString());
        bmpMetadata.SetQuery("/ifd/xmp/exif:FocalLengthIn35mmFilm", focalLengthIn35mmFilm.ToString());
        bmpMetadata.SetQuery("/ifd/xmp/exif:FNumber", fstop.ToString());
        bmpMetadata.SetQuery("/ifd/xmp/tiff:Make", maker);
        bmpMetadata.SetQuery("/ifd/xmp/tiff:Model", model);
        bmpMetadata.SetQuery("/ifd/xmp/exif:ExposureTime", $"{exposureSecs}");
        bmpMetadata.SetQuery("/ifd/xmp/exif:ISOSpeed", $"{iso}");
        bmpMetadata.SetQuery("/ifd/xmp/exif:WhiteBalance", "1");
        bmpMetadata.SetQuery("/ifd/xmp/exif:DateTimeOriginal", DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss"));
        return bmpMetadata;
    }

    static ulong ExifRational(uint numerator, uint denominator) => numerator | ((ulong)denominator << 32);
    static ulong ExifRational(double number) => ExifRational((uint)Math.Round(number * 4000), 4000);

}
