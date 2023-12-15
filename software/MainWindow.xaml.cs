// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Diagnostics;

namespace CameraVision;

public partial class MainWindow : System.Windows.Window
{
    MainWindowVM _vm = new MainWindowVM();
    public MainWindow()
    {
        _vm.Initialize();
        InitializeComponent();
        DataContext = _vm;
    }
    void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.ReadRegisters();
        _vm.ReadMipiRegisters();
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
            if (_vm._rawPixels != null)
            {
                int index = (int)(x + _vm.Image.Width * y);
                if (index < _vm._rawPixels.Length)
                {
                    var raw = _vm._rawPixels[index];
                    tbInfo.Text += $" - Raw10: {raw}";
                }
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