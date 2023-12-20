// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CameraVision;

public partial class MainWindowVM
{
    void DebugLinearity(int x, int[] ys)
    {
        Debug.WriteLine("DebugLinearity");
        var values = new List<ushort>();
        foreach (var y in ys)
        {
            values.AddRange(GetRawAvgBGGR(x, y));
        }
        var b = values.Where((x, i) => i % 4 == 0);
        var g1 = values.Skip(1).Where((x, i) => i % 4 == 0);
        var g2 = values.Skip(2).Where((x, i) => i % 4 == 0);
        var r = values.Skip(3).Where((x, i) => i % 4 == 0);
        foreach (var v in b)
        {
            Debug.Write($"{v} ");
        }
        Debug.WriteLine("");
        foreach (var v in g1)
        {
            Debug.Write($"{v} ");
        }
        Debug.WriteLine("");
        foreach (var v in g2)
        {
            Debug.Write($"{v} ");
        }
        Debug.WriteLine("");
        foreach (var v in r)
        {
            Debug.Write($"{v} ");
        }
        Debug.WriteLine("");
    }

    ushort[] GetRawAvgBGGR(int x, int y)
    {
        // x,y = B coordinates
        return new ushort[] {
            GetRawAvgPixel(x, y),
            GetRawAvgPixel(x + 1, y),
            GetRawAvgPixel(x, y + 1),
            GetRawAvgPixel(x + 1, y + 1)
        };
    }

    ushort[] GetRawBGGR(int x, int y)
    {
        // x,y = B coordinates
        return new ushort[] {
            RawImage.GetPixel(x, y),
            RawImage.GetPixel(x + 1, y),
            RawImage.GetPixel(x, y + 1),
            RawImage.GetPixel(x + 1, y + 1)
        };
    }

    public ushort GetRawAvgPixel(int x, int y, int avgSize = 5)
    {
        double avg = 0;
        int avgCounter = 0;
        for (var xOff = -avgSize; xOff < avgSize; xOff++)
        {
            for (var yOff = -avgSize; yOff < avgSize; yOff++)
            {
                avgCounter++;
                avg += RawImage.GetPixel(x + xOff * 2, y + yOff * 2);
            }
        }
        return (ushort)Math.Round(avg / avgCounter);
    }

    void CreateHistogram(UInt16[] rawPixels)
    {
        int[] histogramValues = new int[65536];
        for (int pixelIndex = 0; pixelIndex < Image.Width * Image.Height; pixelIndex++)
        {
            histogramValues[rawPixels[pixelIndex]]++;
        }
        HistogramPoints.Clear();
        double max = histogramValues.Max();
        HistogramPoints.Add(new Point(0, 0));
        for (int i = 0; i < histogramValues.Length; i++)
        {
            double x = i;
            double y = 100.0 * histogramValues[i] / max;
            HistogramPoints.Add(new Point(x, y));
        }
        double endX = (histogramValues.Length - 1);
        HistogramPoints.Add(new Point(endX, 0));
        HistogramPoints = new PointCollection(HistogramPoints);
        OnPropertyChanged(nameof(HistogramPoints));
    }


    // Experimental function
    [RelayCommand(CanExecute = nameof(IsConnected))]
    public async Task ExposureBracketing()
    {
        IsEnabled = false;
        await SetExposureAndWaitFrame(Exposure);
        await DownloadImage();
        Image.SavePNG("normal.png");

        await SetExposureAndWaitFrame(Exposure * 4);
        await DownloadImage();
        Image.SavePNG("long.png");

        await SetExposureAndWaitFrame(Exposure / 8);
        await DownloadImage();
        Image.SavePNG("short.png");

        // Restore Exposure
        Exposure *= 4;
        IsEnabled = true;
    }

    async Task SetExposureAndWaitFrame(double newExposure)
    {
        // TODO: We are too conservative here, we can speed this up
        var oldExposureMs = ExposureMs;
        Exposure = newExposure;
        // Wait for the old frame to complete
        await Task.Delay((int)Math.Ceiling(oldExposureMs * 4));
        // Wait for the new frame to complete
        await Task.Delay((int)Math.Ceiling(ExposureMs * 4));
        // Wait few extra buffer time to ensure the new frame is captured
        await Task.Delay(250);
    }

    // Experimental function
    [RelayCommand(CanExecute = nameof(IsConnected))]
    public async Task FocusBracketing()
    {
        Focus = 1000;
        while (Focus > 500)
        {
            await DownloadImage();
            Image.SavePNG(Focus + ".png");
            Focus = Focus - 10;
        }
    }

}