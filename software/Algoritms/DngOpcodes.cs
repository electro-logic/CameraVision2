// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Threading.Tasks;

namespace CameraVision.Algoritms;

public class DngOpcodes
{
    // From DngOpcodesEditor
    public static void FixVignetteRadial(RawImage image, double k0, double k1, double k2, double k3, double k4, double ncx, double ncy)
    {
        int x0 = 0; int y0 = 0;
        int x1 = image.Width- 1; int y1 = image.Height - 1;
        double cx = x0 + ncx * (x1 - x0);
        double cy = y0 + ncy * (y1 - y0);
        double mx = Math.Max(Math.Abs(x0 - cx), Math.Abs(x1 - cx));
        double my = Math.Max(Math.Abs(y0 - cy), Math.Abs(y1 - cy));
        double m = Math.Sqrt(mx * mx + my * my);
        Parallel.For(0, image.Height, (y) =>
        {
            for (int x = 0; x < image.Width; x++)
            {
                double r = Math.Sqrt(Math.Pow(x - cx, 2) + Math.Pow(y - cy, 2)) / m;
                double g = 1.0 + k0 * Math.Pow(r, 2) + k1 * Math.Pow(r, 4) + k2 * Math.Pow(r, 6) + k3 * Math.Pow(r, 8) + k4 * Math.Pow(r, 10);
                image.Pixels[x + y * image.Width] = (ushort)Math.Clamp(Math.Round(image.Pixels[x + y * image.Width] * g), 0, 65535);
            }
        });
    }
}