// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Diagnostics;
using System.Windows.Media;

namespace CameraVision;

public partial class MainWindow : System.Windows.Window
{
    MainWindowVM _vm = new MainWindowVM();
    public MainWindow()
    {
        DataContext = _vm;
        InitializeComponent();
    }
    void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var position = e.GetPosition(image);
        // Uniform stretching only
        var scaleRatio = image.Source.Width / image.ActualWidth;
        int x = (int)Math.Floor(position.X * scaleRatio);
        int y = (int)Math.Floor(position.Y * scaleRatio);
        tbInfo.Text = $"X: {x} Y: {y}";
        try
        {
            if (_vm.RawImage.Pixels != null)
            {
                Color rgb = _vm.Image.GetPixel(x, y);
                tbInfo.Text += $" - Rgb: {rgb.R} {rgb.G} {rgb.B}";
                ushort raw = _vm.RawImage.GetPixel(x, y);
                tbInfo.Text += $" - Raw: {raw}";
                ushort avg = _vm.GetRawAvgPixel(x, y, 5);
                tbInfo.Text += $" - Raw Avg5: {avg}";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
    void Image_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        tbInfo.Text = string.Empty;
    }
}