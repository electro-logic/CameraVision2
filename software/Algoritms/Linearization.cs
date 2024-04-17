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
            new[]{  3.4496e-10,  -1.1764e-05,   1.6968e-01,  -1.5023e+03 },
            new[]{ -4.1131e-10,   1.0844e-05,   1.4957e-01,   1.8665e+02 },
            new[]{ -2.5185e-09,  -1.6112e-05,   3.4486e-02,   4.3410e+03 }
        };
        double[][] coefsG1 = {
            new[]{  1.4389e-10,   2.0860e-06,  -8.2587e-02,  -7.7096e+02 },
            new[]{ -5.1828e-10,   1.1516e-05,   2.1455e-01,  -7.9601e+01 },
            new[]{ -2.0944e-09,  -2.2450e-05,  -2.4319e-02,   4.6997e+03 }
        };
        double[][] coefsG2 = {
            new[]{  1.3170e-10,   2.8918e-06,  -9.6886e-02,  -7.2907e+02 },
            new[]{ -5.2314e-10,   1.1523e-05,   2.1800e-01,  -9.2631e+01 },
            new[]{ -2.0703e-09,  -2.2762e-05,  -2.7521e-02,   4.7148e+03 }
        };
        double[][] coefsR = {
            new[]{  1.7150e-10,  -3.3015e-06,   1.5836e-02,  -6.5265e+02 },
            new[]{ -3.0329e-10,   7.9381e-06,   1.1712e-01,  -9.4339e+01 },
            new[]{ -1.7330e-09,  -1.1938e-05,   2.9734e-02,   3.0906e+03 }
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