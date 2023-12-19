﻿// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using CameraVision.Algoritms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraVision;

public partial class MainWindowVM : ObservableObject
{
    const string maker = "CameraVision2";
    const string model = "D8M";
    const bool DNG_COLOR_MATRIX = false;

    OV8865 _sensor;

    VideoSetting _currentVideoSetting;
    PixelFormat _pixelFormat = PixelFormats.Rgba64;
    [ObservableProperty]
    PointCollection _histogramPoints = new PointCollection();
    [ObservableProperty]
    DemosaicingAlgorithms _currentDemosaicingAlgorithm = Algoritms.DemosaicingAlgorithms.SIMPLE_INTERPOLATION;
    [ObservableProperty]
    Visibility _progressVisibility = Visibility.Collapsed;
    [ObservableProperty]
    WriteableBitmap _image;
    [ObservableProperty]
    double _downloadProgress;
    [ObservableProperty]
    bool _isEnabled = true;
    [ObservableProperty]
    int _minimumValue, _maximumValue;
    [ObservableProperty]
    bool _isLinearizedEnabled = false;
    [ObservableProperty]
    bool _isLensShadingCorrectionEnabled = true;
    [ObservableProperty]
    bool _isConnected = false;

    [ObservableProperty]
    RawImage _rawImage;

    public List<VideoSetting> VideoSettings { get; set; }
    public DemosaicingAlgorithms[] DemosaicingAlgorithms => (DemosaicingAlgorithms[])Enum.GetValues(typeof(DemosaicingAlgorithms));
    public OV8865.COM[] Communications => new OV8865.COM[] { OV8865.COM.COM_JTAG, OV8865.COM.COM_FT232H, OV8865.COM.COM_NONE };

    [RelayCommand]
    public void Connect()
    {
        if (IsConnected)
        {
            MessageBox.Show("Already connected");
            return;
        }
        try
        {
            _sensor = new OV8865();
            IsConnected = true;
            Communication = OV8865.COM.COM_JTAG;
            VideoSettings = JsonSerializer.Deserialize<List<VideoSetting>>(File.ReadAllText(@"Settings\CameraSettings.json"));
            Thread.Sleep(250);
            // Hardcoded default settings
            CurrentVideoSetting = VideoSettings[6];
            Focus = 128;
            IsWhiteBalanceEnabled = true;
            MWBGainBlue = MWBGainGreen = MWBGainRed = 1.0;
            ReadRegisters();
            ReadMipiRegisters();
            Communication = OV8865.COM.COM_FT232H;
        }
        catch
        {
            Communication = OV8865.COM.COM_NONE;
            IsConnected = false;
            MessageBox.Show("Connection failed");
        }
    }

    public VideoSetting CurrentVideoSetting
    {
        get => _currentVideoSetting;
        set
        {
            if (!IsConnected)
                return;

            const int DELAY = 100;
            _currentVideoSetting = value;
            OnPropertyChanged();
            //_sensor.WriteReg(0x0100, 0x00); // Software Standby                
            // No skipping or binning                
            _sensor.WriteReg(0x3814, 0x01); Thread.Sleep(DELAY);
            _sensor.WriteReg(0x3815, 0x01); Thread.Sleep(DELAY);
            _sensor.WriteReg(0x382A, 0x01); Thread.Sleep(DELAY);
            _sensor.WriteReg(0x382B, 0x01); Thread.Sleep(DELAY);
            _sensor.WriteReg(0x3821, 0x40); // FORMAT2 = hsync_en_o
            _sensor.WriteReg(0x3830, 0x08); // BLC NUM OPTION
            _sensor.WriteReg(0x3836, 0x02); // ZLINE NUM OPTION
            //_sensor.WriteReg(0x0100, 0x01); // Streaming
            foreach (var reg in _currentVideoSetting.Registers)
            {
                _sensor.WriteReg(reg.Address, (byte)reg.Value);
                if ((reg.Address == 0x3814) | (reg.Address == 0x3815) | (reg.Address == 0x382A) | (reg.Address == 0x382B))
                {
                    Thread.Sleep(DELAY);
                }
            }
            int ImageWidthH = _currentVideoSetting.Registers.Where((r) => r.Address == 0x3808).First().Value;
            int ImageWidthL = _currentVideoSetting.Registers.Where((r) => r.Address == 0x3809).First().Value;
            int ImageWidth = (ImageWidthH << 8) | ImageWidthL;
            int ImageHeightH = _currentVideoSetting.Registers.Where((r) => r.Address == 0x380A).First().Value;
            int ImageHeightL = _currentVideoSetting.Registers.Where((r) => r.Address == 0x380B).First().Value;
            int ImageHeight = (ImageHeightH << 8) | ImageHeightL;
            _sensor.Config((ushort)ImageWidth, (ushort)ImageHeight);
            Image = new WriteableBitmap(ImageWidth, ImageHeight, 96, 96, _pixelFormat, null);
            RawImage=new RawImage(ImageWidth, ImageHeight);
        }
    }

    void UpdateProgress(double progress)
    {
        DownloadProgress = progress;
        if ((progress > 0.0) && (progress < 100.0))
        {
            ProgressVisibility = Visibility.Visible;
        }
        else
        {
            ProgressVisibility = Visibility.Collapsed;
        }
    }

    BitmapMetadata GetTIFFMetadataD8M() => Metadata.GetTIFFMetadata(maker, model, 3.37, 2.8, 4.6144, 3.472, ExposureMs, (UInt16)ISO);

    [RelayCommand(CanExecute = nameof(IsConnected))]
    public void ReadRegisters()
    {
        Registers = new ObservableCollection<Register>(
            JsonSerializer.Deserialize<List<Register>>(File.ReadAllText(@"Settings\OV8865_Common.json"))
            .Concat(JsonSerializer.Deserialize<List<Register>>(File.ReadAllText(@"Settings\OV8865_MIPI.json")))
        );
        foreach (var register in Registers)
        {
            register.Value = _sensor.ReadReg(register.Address);
            register.PropertyChanged += (sender, e) =>
            {
                var reg = sender as Register;
                _sensor.WriteReg(reg.Address, (byte)reg.Value);
            };
        }
        // Update GUI elements
        OnPropertyChanged(nameof(FPS));
        OnPropertyChanged(nameof(ExposureMs)); OnPropertyChanged(nameof(Exposure));
        OnPropertyChanged(nameof(AnalogGain)); OnPropertyChanged(nameof(ISO));
        OnPropertyChanged(nameof(IsWhiteBalanceEnabled));
        OnPropertyChanged(nameof(MWBGainRed)); OnPropertyChanged(nameof(MWBGainGreen)); OnPropertyChanged(nameof(MWBGainBlue));
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    public void ReadMipiRegisters()
    {
        MipiRegisters = new ObservableCollection<Register>(
            JsonSerializer.Deserialize<List<Register>>(File.ReadAllText(@"Settings\TC358748XBG_Common.json"))
            .Concat(JsonSerializer.Deserialize<List<Register>>(File.ReadAllText(@"Settings\TC358748XBG_Debug.json")))
        );
        foreach (var register in MipiRegisters)
        {
            register.Value = _sensor.ReadRegMipi(register.Address);
            register.PropertyChanged += (sender, e) =>
            {
                var reg = sender as Register;
                _sensor.WriteRegMipi(reg.Address, reg.Value);
            };
        }
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    public async Task DownloadImage()
    {
        try
        {
            IsEnabled = false;
            UpdateImage(await _sensor.GetImage(Image.PixelWidth, Image.PixelHeight, new Progress<double>(UpdateProgress)));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            MessageBox.Show($"{ex.ToString()}");
        }
        IsEnabled = true;
    }

    void UpdateImage(ushort[] rawPixels)
    {
        RawImage.Pixels = rawPixels;
        Debug.WriteLine("Frame received");
        int imageWidth = Image.PixelWidth;
        int imageHeight = Image.PixelHeight;
        if (IsLinearizedEnabled)
        {
            Linearization.Linearize(RawImage);
            Debug.WriteLine("Raw pixels linearized");
        }
        MinimumValue = RawImage.Pixels.Min();
        MaximumValue = RawImage.Pixels.Max();
        //CreateHistogram(RawPixels);

        var rawPreview = RawImage.Clone();
        if (IsLensShadingCorrectionEnabled)
        {
            // Parameters manually determined with DngOpcodesEditor
            DngOpcodes.FixVignetteRadial(rawPreview, 1.8, 0, 0, 0, 0, 0.52, 0.41);
        }
        ulong[] colorPixels = null;
        switch (CurrentDemosaicingAlgorithm)
        {
            case Algoritms.DemosaicingAlgorithms.SIMPLE_INTERPOLATION:
                colorPixels = Bayer.Demosaic(rawPreview);
                break;
            case Algoritms.DemosaicingAlgorithms.BGGR_BAYER_RAW:
                colorPixels = Bayer.ColorRaw(rawPreview);
                break;
            case Algoritms.DemosaicingAlgorithms.GRAY16_RAW:
                colorPixels = Bayer.GrayRaw(rawPreview);
                break;
        }
        Image.WritePixels(new Int32Rect(0, 0, imageWidth, imageHeight), colorPixels, imageWidth * _pixelFormat.BitsPerPixel / 8, 0);
    }

    [RelayCommand]
    public void SaveTiffImage()
    {
        var dlg = new SaveFileDialog();
        dlg.FileName = "camera_" + DateTime.Now.ToString("hhmmss") + ".tiff";
        dlg.DefaultExt = ".tiff";
        dlg.Filter = "TIFF image (.tiff)|*.tiff";
        if (dlg.ShowDialog() == true)
        {
            SaveTiff(dlg.FileName);
        }
    }

    [RelayCommand]
    public void SaveDngImage()
    {
        var dlg = new SaveFileDialog();
        dlg.FileName = "camera_" + DateTime.Now.ToString("hhmmss") + ".dng";
        dlg.DefaultExt = ".dng";
        dlg.Filter = "DNG image (.dng)|*.dng";
        if (dlg.ShowDialog() == true)
        {
            SaveDng(dlg.FileName);
        }
    }

    void SaveTiff(string filename) => RawImage.SaveTiff(filename, GetTIFFMetadataD8M());

    void SaveDng(string filename)
    {
        // Save RAW (two steps)
        // Step 1: Create a RAW TIFF file
        var filenameTiff = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "_raw.tiff");
        RawImage.SaveTiff(filenameTiff, GetTIFFMetadataD8M(), TiffCompressOption.None);
        // Step 2: Add DNG tags and save as .DNG
        TIFFtoDNG(filenameTiff, filename, IsWhiteBalanceEnabled);
    }

    void TIFFtoDNG(string filenameTiff, string filename, bool IsWhiteBalanceEnabled = false)
    {
        // We use ExifTool https://exiftool.org because of BitmapMetadata limitations
        // The tool dng_validate.exe is available with the DNG SDK and can be used to validate the DNG file.
        // Please Note: The following metadata don't fully characterize the camera. Add your camera calibration data
        // (LinearizationTable,CalibrationIlluminant1,ColorMatrix1,etc..) to have a DNG profile that renders nice straight out of the box.
        // A Lens profile can also be built by using "Adobe Lens Profile Creator" or similar software to correct the geometry.
        string identityMatrix = "1.0 0.0 0.0 0.0 1.0 0.0 0.0 0.0 1.0";

        // D8M Preliminary Color Matrices
        const string CalibrationIlluminant1 = "Tungsten (Incandescent)";    // // https://exiftool.org/TagNames/EXIF.html#LightSource
        const string ColorMatrix1 = "1.6572 -0.5447 -0.0647 0.4383 0.1854 0.1173 0.0805 0.0367 0.3337";
        const string CalibrationIlluminant2 = "D50";
        const string ColorMatrix2 = "1.3744 -0.4354 0.1334 0.1255 0.6122 0.3058 0.0622 0.0897 0.4731";
        const string CalibrationIlluminant3 = "Warm White Fluorescent";   // CCT 2566K
        const string ColorMatrix3 = "1.4365 -0.513 0.0944 0.0585 0.7667 0.2145 0.0607 0.0998 0.4381";
        const string AsShotNeutral = "0.5439 1.0000 0.5965";  // RGB White Balance for D50
        string dngMetadata =
            "-DNGVersion=1.6.0.0 " +
            "-EXIF:SubfileType=\"Full-resolution Image\" " +
            "-PhotometricInterpretation=\"Color Filter Array\" " +  // Bayer Pattern Image
            "-IFD0:CFARepeatPatternDim=\"2 2\" " +                  // Bayer Pattern Size: 2x2
            "-IFD0:CFAPattern2=\"2 1 1 0\" " +                      // Bayer Pattern: BGGR
            "-Orientation=Horizontal " +
            $"-UniqueCameraModel=\"{maker} {model}\" ";
        if (DNG_COLOR_MATRIX)
        {
            dngMetadata += $"-CalibrationIlluminant1=\"{CalibrationIlluminant1}\" " +
            $"-ColorMatrix1=\"{ColorMatrix1}\" " +
            $"-CalibrationIlluminant2=\"{CalibrationIlluminant2}\" " +
            $"-ColorMatrix2=\"{ColorMatrix2}\" " +
            $"-CalibrationIlluminant3=\"{CalibrationIlluminant3}\" " +
            $"-ColorMatrix3=\"{ColorMatrix3}\" " +                  // ColorMatrix3 supported in DNG 1.6 and later
            $"-AsShotNeutral=\"{AsShotNeutral}\" ";
        }
        else
        {
            dngMetadata += $"-ColorMatrix1=\"{identityMatrix}\" ";
        }
        if (IsWhiteBalanceEnabled)
        {
            dngMetadata += $"-AnalogBalance=\"{MWBGainRed} {MWBGainGreen} {MWBGainBlue}\" ";
        }
        ExifTool.RunExifTool($"{dngMetadata} -o \"{filename}\" \"{filenameTiff}\"");
        //File.Delete(filenameTiff);
    }

    [RelayCommand]
    public void OpenImage()
    {
        var dlg = new OpenFileDialog();
        dlg.Multiselect = true;
        dlg.DefaultExt = ".dng";
        dlg.Filter = "RAW image|*.dng;*.tiff;*.tif";
        if (dlg.ShowDialog() == true)
        {
            string filename = dlg.FileName;
            if (Path.GetExtension(filename).ToLower() == ".dng")
            {
                string filenameTIFF = filename + ".tiff";
                ExifTool.DNGtoTIFF(filename, filenameTIFF);
                filename = filenameTIFF;
            }
            var decoder = BitmapDecoder.Create(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            var w = frame.PixelWidth;
            var h = frame.PixelHeight;

            if (frame.Format.BitsPerPixel != 16)
                return;

            Debug.Assert(frame.Format.BitsPerPixel == 16);

            //CurrentDemosaicingAlgorithm = Algoritms.DemosaicingAlgorithms.BGGR_BAYER_RAW;
            RawImage = new RawImage(w, h);            
            frame.CopyPixels(RawImage.Pixels, w * 2, 0);
            Image = new WriteableBitmap(w, h, 96, 96, _pixelFormat, null);
            UpdateImage(RawImage.Pixels);
            //DebugLinearity(1228, new[] { 852, 762, 672, 584, 492, 400 });
            //DebugLinearity(714, new[] { 352, 428, 506, 582, 658, 734, 810, 886 });
        }
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    public void Reset()
    {
        _sensor.Reset();
        Application.Current.Shutdown();
    }
}