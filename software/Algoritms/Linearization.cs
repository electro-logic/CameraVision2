// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Threading.Tasks;

namespace CameraVision.Algoritms;

public static class Linearization
{
    static double EvalPiecedPolynomial(double x, double[] breaks, double[][] coefs)
    {
        var polyIndex = (int)Math.Min(Math.Floor(x / breaks[1]), breaks.Length - 2);
        var coef = coefs[polyIndex];
        var x0 = breaks[polyIndex];
        return coef[0] * Math.Pow(x - x0, 3) + coef[1] * Math.Pow(x - x0, 2) + coef[2] * (x - x0) + coef[3];
    }
    public static void Linearize(RawImage image)
    {
        // Algorithm not recommended for production

        // We linearize channel responses with three polynomials pieced together (splines)
        // The correction is obtained by subtracting the polynomial regression from the linear regression 
        double[] breaks = { 0, 21845, 43691, 65536 };
        
        // TODO: coefficients need to be calculated
        double[][] coefsB = {
            new[]{  3.5568e-10,  -1.3610e-05,   2.4910e-01,  -1.5629e+03 },
            new[]{ -3.7282e-10,   9.7001e-06,   1.6370e-01,   1.0920e+03 },
            new[]{ -1.8201e-09,  -1.4733e-05,   5.3748e-02,   5.4104e+03 }
        };
        double[][] coefsG1 = {
            new[]{  1.5683e-11,   6.6319e-06,  -8.1041e-02,  -8.4337e+02 },
            new[]{ -4.3024e-10,   7.6597e-06,   2.3116e-01,   7.1463e+02 },
            new[]{ -7.8755e-10,  -2.0537e-05,  -5.0139e-02,   4.9346e+03 }
        };
        double[][] coefsG2 = {
            new[]{  1.3161e-11,   6.7865e-06,  -8.2550e-02,  -8.4776e+02 },
            new[]{ -4.3239e-10,   7.6490e-06,   2.3280e-01,   7.2475e+02 },
            new[]{ -7.7933e-10,  -2.0688e-05,  -5.2047e-02,   4.9529e+03 }
        };
        double[][] coefsR = {
            new[]{  2.1120e-10,  -6.9575e-06,   1.2922e-01,  -1.0204e+03 },
            new[]{ -2.8367e-10,   6.8834e-06,   1.2760e-01,   6.8397e+02 },
            new[]{ -1.1232e-09,  -1.1707e-05,   2.2227e-02,   3.7991e+03 }
        };
        Parallel.For(0, image.Height, (py) =>
        {
            for (int px = 0; px < image.Width; px++)
            {
                var x = image.GetPixel(px, py);
                double y = 0;
                bool rowEven = py % 2 == 0;
                bool colEven = px % 2 == 0;
                if (rowEven && colEven)
                {
                    // b
                    y = EvalPiecedPolynomial(x, breaks, coefsB);
                }
                if (!rowEven && colEven)
                {
                    // g1
                    y = EvalPiecedPolynomial(x, breaks, coefsG1);
                }
                if (rowEven && !colEven)
                {
                    // g2
                    y = EvalPiecedPolynomial(x, breaks, coefsG2);
                }
                if (!rowEven && !colEven)
                {
                    // r
                    y = EvalPiecedPolynomial(x, breaks, coefsR);
                }
                var uy = (ushort)Math.Clamp(Math.Round(x + y), 0, ushort.MaxValue);
                image.SetPixel(px, py, uy);
            }
        });
    }
}