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
        return coef[0] * x * x * x + coef[1] * x * x + coef[2] * x + coef[3];
    }
    public static ushort[] Linearize(ushort[] pixels, int imageWidth, int imageHeight)
    {
        // Algorithm not recommended for production

        // We linearize channel responses with three polynomials pieced together (splines)
        // The correction is obtained by subtracting the polynomial regression from the linear regression 
        double[] breaks = { 0, 21845, 43691, 65536 };
        
        // TODO: coefficients need to be calculated
        double[][] coefsB = {
            new[]{  5.3198e-10, -2.2160e-05,   1.8638e-01,  -3.4929e+00 },
            new[]{ -2.7856e-10,  1.2704e-05,  -2.0179e-02,  -9.6112e+02 },
            new[]{ -9.9967e-10, -5.5513e-06,   1.3608e-01,   1.7568e+03 }
        };
        double[][] coefsG1 = {
            new[]{ -3.6824e-11,   3.3687e-06,  -9.8925e-02,   5.3462e+02 },
            new[]{ -1.7058e-11,   9.5538e-07,  -4.4642e-03,  -4.0272e+02 },
            new[]{  1.6112e-10,  -1.6252e-07,   1.2856e-02, - 2.2215e+02 }
        };
        double[][] coefsG2 = {
            new[]{  1.7835e-10,  -5.1598e-06,  -1.4379e-02,   2.9412e+02 },
            new[]{ -1.8598e-10,   6.5288e-06,   1.5528e-02,  -6.2301e+02 },
            new[]{ -1.4488e-10,  -5.6594e-06,   3.4521e-02,   8.9307e+02 }
        };
        double[][] coefsR = {
            new[]{ -4.0306e-10,   1.8340e-05,  -2.4127e-01,   7.7748e+02 },
            new[]{  2.2013e-10,  -8.0743e-06,  -1.7011e-02,   5.7241e+01 },
            new[]{  8.4445e-10,   6.3519e-06,  -5.4638e-02,  -1.8728e+03 }
        };
        Parallel.For(0, imageHeight, (py) =>
        {
            for (int px = 0; px < imageWidth; px++)
            {
                int pixelIndex = py * imageWidth + px;
                var x = pixels[pixelIndex];
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
                pixels[pixelIndex] = (ushort)Math.Clamp(Math.Round(x + y), 0, 65535);
            }
        });
        return pixels;
    }
}