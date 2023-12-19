// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Threading.Tasks;

namespace CameraVision.Algoritms;

public enum DemosaicingAlgorithms
{
    SIMPLE_INTERPOLATION,
    BGGR_BAYER_RAW,
    GRAY16_RAW
}

/// <summary>
/// Algorithms that works on byte array of raw bayer pattern
/// </summary>
public static class Bayer
{
    /// <summary>
    /// Demosaicize a RAW image (10 bit BGGR pattern) into a RGBA64 image.
    /// Low-performance and simple interpolation pattern. Algorithm not recommended for production.
    /// </summary>
    public static ulong[] Demosaic(ushort[] imageRaw, int imageWidth, int imageHeight)
    {
        var imageColor = new ulong[imageWidth * imageHeight];

        //for (int y = 0; y< imageHeight; y++)
        Parallel.For(0, imageHeight, (y) =>
        {
            for (int x = 0; x < imageWidth; x++)
            {
                int pixelIndex = y * imageWidth + x;
                ushort color = imageRaw[pixelIndex];
                ushort r = 0, g = 0, b = 0;
                if (x > 0 && x < imageWidth - 1 && y > 0 && y < imageHeight - 1)
                {
                    ushort g1 = 0, g2 = 0, g3 = 0, g4 = 0;
                    ushort r1 = 0, r2 = 0, r3 = 0, r4 = 0;
                    ushort b1 = 0, b2 = 0, b3 = 0, b4 = 0;
                    bool rowEven = y % 2 == 0;
                    bool colEven = x % 2 == 0;
                    if (rowEven && colEven)
                    {
                        g1 = imageRaw[y * imageWidth + (x - 1)];
                        g2 = imageRaw[y * imageWidth + x + 1];
                        g3 = imageRaw[(y + 1) * imageWidth + x];
                        g4 = imageRaw[(y - 1) * imageWidth + x];
                        r1 = imageRaw[(y - 1) * imageWidth + (x - 1)];
                        r2 = imageRaw[(y + 1) * imageWidth + (x - 1)];
                        r3 = imageRaw[(y - 1) * imageWidth + x + 1];
                        r4 = imageRaw[(y + 1) * imageWidth + x + 1];
                        r = (ushort)((r1 + r2 + r3 + r4) / 4);
                        g = (ushort)((g1 + g2 + g3 + g4) / 4);
                        b = color;
                    }
                    if (!rowEven && colEven)
                    {
                        r1 = imageRaw[y * imageWidth + (x - 1)];
                        r2 = imageRaw[y * imageWidth + x + 1];
                        b1 = imageRaw[(y - 1) * imageWidth + x];
                        b2 = imageRaw[(y + 1) * imageWidth + x];
                        r = (ushort)((r1 + r2) / 2);
                        g = color;
                        b = (ushort)((b1 + b2) / 2);
                    }
                    if (rowEven && !colEven)
                    {
                        b1 = imageRaw[y * imageWidth + (x - 1)];
                        b2 = imageRaw[y * imageWidth + x + 1];
                        r1 = imageRaw[(y - 1) * imageWidth + x];
                        r2 = imageRaw[(y + 1) * imageWidth + x];
                        r = (ushort)((r1 + r2) / 2);
                        g = color;
                        b = (ushort)((b1 + b2) / 2);
                    }
                    if (!rowEven && !colEven)
                    {
                        g1 = imageRaw[y * imageWidth + (x - 1)];
                        g2 = imageRaw[y * imageWidth + x + 1];
                        g3 = imageRaw[(y - 1) * imageWidth + x];
                        g4 = imageRaw[(y + 1) * imageWidth + x];
                        b1 = imageRaw[(y - 1) * imageWidth + (x - 1)];
                        b2 = imageRaw[(y + 1) * imageWidth + (x - 1)];
                        b3 = imageRaw[(y - 1) * imageWidth + x + 1];
                        b4 = imageRaw[(y + 1) * imageWidth + x + 1];
                        r = color;
                        g = (ushort)((g1 + g2 + g3 + g4) / 4.0);
                        b = (ushort)((b1 + b2 + b3 + b4) / 4.0);
                    }
                }
                //RGBA64
                // ARGB = 0xFFFFRRRRGGGGBBBB
                imageColor[pixelIndex] = r | (ulong)g << 16 | (ulong)b << 32 | (ulong)ushort.MaxValue << 48;
            }
        }
        );
        return imageColor;
    }
    /// <summary>
    /// Simple Bayer to Gray conversion. Algorithm not recommended for production.
    /// </summary>
    public static ulong[] GrayRaw(ushort[] imageRaw, int imageWidth, int imageHeight)
    {
        var imageColor = new ulong[imageWidth * imageHeight];
        Parallel.For(0, imageHeight, (y) =>
        {
            for (int x = 0; x < imageWidth; x++)
            {
                int pixelIndex = y * imageWidth + x;
                ushort color = imageRaw[pixelIndex];
                imageColor[pixelIndex] = color | (ulong)color << 16 | (ulong)color << 32 | (ulong)ushort.MaxValue << 48;
            }
        });
        return imageColor;
    }
    /// <summary>
    /// Didactic implementation to view Bayer pattern. Algorithm not recommended for production.
    /// </summary>
    public static ulong[] ColorRaw(ushort[] imageRaw, int imageWidth, int imageHeight)
    {
        var imageColor = new ulong[imageWidth * imageHeight];
        Parallel.For(0, imageHeight, (y) =>
        {
            for (int x = 0; x < imageWidth; x++)
            {
                int pixelIndex = y * imageWidth + x;
                ushort color = imageRaw[pixelIndex];
                bool rowEven = y % 2 == 0;
                bool colEven = x % 2 == 0;
                ushort r = 0, g = 0, b = 0;
                if (rowEven && colEven)
                {
                    r = 0; g = 0; b = color;
                }
                if (!rowEven && colEven)
                {
                    r = 0; g = color; b = 0;
                }
                if (rowEven && !colEven)
                {
                    r = 0; g = color; b = 0;
                }
                if (!rowEven && !colEven)
                {
                    r = color; g = 0; b = 0;
                }
                imageColor[pixelIndex] = BitConverter.ToUInt64(new byte[] {
                    (byte)(r & 0xFF), (byte)((r & 0xFF00) >> 8),
                    (byte)(g & 0xFF), (byte)((g & 0xFF00) >> 8),
                    (byte)(b & 0xFF), (byte)((b & 0xFF00) >> 8),
                    0xFF, 0xFF
                }, 0);
            }
        });
        return imageColor;
    }
}