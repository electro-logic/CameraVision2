// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

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
    OV8865 _sensor;

    VideoSetting _currentVideoSetting;
    PixelFormat _pixelFormat = PixelFormats.Rgba64;
    [ObservableProperty]
    PointCollection _histogramPoints = new PointCollection();
    [ObservableProperty]
    DemosaicingAlgorithms _currentDemosaicingAlgorithm = CameraVision.DemosaicingAlgorithms.SIMPLE_INTERPOLATION;
    [ObservableProperty]
    Visibility _progressVisibility = Visibility.Collapsed;
    [ObservableProperty]
    WriteableBitmap _image;
    [ObservableProperty]
    double _downloadProgress;
    [ObservableProperty]
    bool _isEnabled;
    [ObservableProperty]
    int _minimumValue, _maximumValue;
    [ObservableProperty]
    UInt16[] _rawPixels;

    OV8865.COM _communication = OV8865.COM.COM_JTAG;

    public OV8865.COM Communication
    {
        get => _sensor.ReadCom();
        set
        {
            if (SetProperty(ref _communication, value))
            {
                _sensor.WriteCom(_communication);
                _sensor.SyncCom();
            }
        }
    }

    [ObservableProperty]
    ObservableCollection<Register> _registers = new ObservableCollection<Register>();
    [ObservableProperty]
    ObservableCollection<Register> _mipiRegisters = new ObservableCollection<Register>();
    
    public List<VideoSetting> VideoSettings { get; set; }
    public DemosaicingAlgorithms[] DemosaicingAlgorithms => (DemosaicingAlgorithms[])Enum.GetValues(typeof(DemosaicingAlgorithms));
    public OV8865.COM[] Communications => new OV8865.COM[] { OV8865.COM.COM_JTAG, OV8865.COM.COM_FT232H };

    public void Initialize()
    {
        try
        {
            _sensor = new OV8865();
            IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\nFPGA with D8M and the bitstream loaded is connected?");
            Environment.Exit(-1);
        }
        Communication = OV8865.COM.COM_JTAG;
        VideoSettings = JsonSerializer.Deserialize<List<VideoSetting>>(File.ReadAllText(@"Settings\CameraSettings.json"));
        
        // Hardcoded default settings
        CurrentVideoSetting = VideoSettings[6];
        Focus = 160;
        IsWhiteBalanceEnabled = true;
        MWBGainBlue = MWBGainGreen = MWBGainRed = 1.0;
        Communication = OV8865.COM.COM_FT232H;

        // Check Camera with a Color Bar
        //await Task.Delay(250);
        //_sensor.WriteReg(0x5E00, 0x80); // Color Bar
        //_sensor.WriteReg(0x5E00, 0x92); // Square Color Bar            
    }

    public double Exposure
    {
        get => (double)(_sensor.GetExposure());
        set
        {
            _sensor.SetExposure((UInt16)(value));

            // TODO: Check upper limit to exposure/vts/hts settings
            UInt16 vts = _sensor.GetVTS();  // dummy lines
            UInt16 hts = _sensor.GetHTS();  // extra lines

            // Max exposure = dummy lines - 4
            UInt16 maxExposure = (UInt16)(vts);

            // Exposure = Shutter + extra lines 
            UInt16 exposure = (UInt16)(value + hts);
            if (maxExposure < exposure)
            {
                _sensor.SetVTS((UInt16)(exposure + 4));
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ExposureMs));
        }
    }

    public double Focus
    {
        get => (double)(_sensor.GetFocus());
        set { _sensor.SetFocus((UInt16)value); OnPropertyChanged(); }
    }

    public double AnalogGain
    {
        get => (double)(_sensor.GetAnalogGain() + 1);
        set { _sensor.SetAnalogGain((byte)(value - 1)); OnPropertyChanged(); OnPropertyChanged(nameof(ISO)); }
    }

    public double ISO => AnalogGain * 100;

    public double MWBGainRed
    {
        get => (double)(_sensor.GetMWBGainRed() / 1024);
        set { _sensor.SetMWBGainRed((UInt16)(value * 1024)); OnPropertyChanged(); }
    }

    public double MWBGainGreen
    {
        get => (double)(_sensor.GetMWBGainGreen() / 1024);
        set { _sensor.SetMWBGainGreen((UInt16)(value * 1024)); OnPropertyChanged(); }
    }

    public double MWBGainBlue
    {
        get => (double)(_sensor.GetMWBGainBlue() / 1024);
        set { _sensor.SetMWBGainBlue((UInt16)(value * 1024)); OnPropertyChanged(); }
    }

    public VideoSetting CurrentVideoSetting
    {
        get => _currentVideoSetting;
        set
        {
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
        }
    }
    public bool IsWhiteBalanceEnabled
    {
        get => _sensor.GetWhiteBalanceEnable();
        set
        {
            _sensor.SetWhiteBalanceEnable(value);
            OnPropertyChanged();
        }
    }
    // For optimal performance exposure should be <200ms
    public double ExposureMs
    {
        get
        {
            UInt16 hts = _sensor.GetHTS();  // extra lines
            return Math.Round((1.0 / 125000.0) * (double)(hts) * (Exposure), 2);
        }
    }
    public double FPS
    {
        get
        {
            UInt16 hts = _sensor.GetHTS();  // extra lines
            UInt16 vts = _sensor.GetVTS();  // dummy lines
            // System Clock (SCLK) of sensor is set at 166.66 MHz
            double fps = 166666666.0 / (hts * vts);
            return Math.Round(fps, 2);
        }
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

    const string maker = "CameraVision2";
    const string model = "D8M";
    BitmapMetadata GetTIFFMetadataD8M()
    {
        // D8M / OV8865 specs
        const double f = 3.37;
        const double fstop = 2.8;
        const double sensorWidth = 4.6144;
        const double sensorHeight = 3.472;
        double sensor35Diagonal = Math.Sqrt(36 * 36 + 24 * 24);
        double sensorDiagonal = Math.Sqrt(sensorWidth * sensorWidth + sensorHeight * sensorHeight);
        double cropFactor = sensor35Diagonal / sensorDiagonal;
        double focalLengthIn35mmFilm = cropFactor * f;
        double exposureSecs = ExposureMs / 1000.0;
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
        bmpMetadata.SetQuery("/ifd/exif/{ushort=34855}", (UInt16)ISO);
        bmpMetadata.SetQuery("/ifd/exif/{ushort=41987}", (ushort)1);                                     // White Balance (0 = Auto white balance, 1 = Manual white balance)
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
        bmpMetadata.SetQuery("/ifd/xmp/exif:ISOSpeed", $"{(UInt16)ISO}");
        bmpMetadata.SetQuery("/ifd/xmp/exif:WhiteBalance", "1");
        bmpMetadata.SetQuery("/ifd/xmp/exif:DateTimeOriginal", DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss"));
        return bmpMetadata;
    }
    ulong ExifRational(uint numerator, uint denominator) => numerator | ((ulong)denominator << 32);
    ulong ExifRational(double number) => ExifRational((uint)Math.Round(number * 4000), 4000);

    [RelayCommand]
    public void ReadRegisters()
    {
        Registers = JsonSerializer.Deserialize<ObservableCollection<Register>>(File.ReadAllText(@"Settings\OV8865_Common.json"));
        foreach (var register in Registers)
        {
            register.Value = _sensor.ReadReg(register.Address);
            register.PropertyChanged += (sender, e) => {
                var reg = sender as Register;
                _sensor.WriteReg(reg.Address, (byte)reg.Value);
            }; ;
        }
        // Update GUI elements
        OnPropertyChanged(nameof(FPS));
        OnPropertyChanged(nameof(ExposureMs)); OnPropertyChanged(nameof(Exposure));
        OnPropertyChanged(nameof(AnalogGain)); OnPropertyChanged(nameof(ISO));
        OnPropertyChanged(nameof(IsWhiteBalanceEnabled));
        OnPropertyChanged(nameof(MWBGainRed)); OnPropertyChanged(nameof(MWBGainGreen)); OnPropertyChanged(nameof(MWBGainBlue));
    }

    [RelayCommand]
    public void ReadMipiRegisters()
    {
        MipiRegisters = JsonSerializer.Deserialize<ObservableCollection<Register>>(File.ReadAllText(@"Settings\TC358748XBG_Common.json"));
        foreach (var register in MipiRegisters)
        {
            register.Value = _sensor.ReadRegMipi(register.Address);
            register.PropertyChanged += (sender, e)=> {
                var reg = sender as Register;
                _sensor.WriteRegMipi(reg.Address, reg.Value);
            };
        }
    }

    [RelayCommand]
    public async Task DownloadImage()
    {
        try
        {
            IsEnabled = false;
            int imageWidth = (int)Image.Width;
            int imageHeight = (int)Image.Height;
            RawPixels = await _sensor.GetImage(imageWidth, imageHeight, new Progress<double>(UpdateProgress));
            Debug.WriteLine("Frame received");
            MinimumValue = RawPixels.Min();
            MaximumValue = RawPixels.Max();
            //CreateHistogram(RawPixels);
            ulong[] colorPixels = null;
            switch (CurrentDemosaicingAlgorithm)
            {
                case CameraVision.DemosaicingAlgorithms.SIMPLE_INTERPOLATION:
                    colorPixels = BayerAlgoritms.Demosaic(RawPixels, imageWidth, imageHeight);
                    break;
                case CameraVision.DemosaicingAlgorithms.BGGR_BAYER_RAW:
                    colorPixels = BayerAlgoritms.ColorRaw(RawPixels, imageWidth, imageHeight);
                    break;
                case CameraVision.DemosaicingAlgorithms.GRAY16_RAW:
                    colorPixels = BayerAlgoritms.GrayRaw(RawPixels, imageWidth, imageHeight);
                    break;
                default:
                    throw new Exception("Demosaicing algorithm not implemented");
            }
            Image.WritePixels(new Int32Rect(0, 0, imageWidth, imageHeight), colorPixels, imageWidth * _pixelFormat.BitsPerPixel / 8, 0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            MessageBox.Show($"{ex.ToString()}");
        }
        IsEnabled = true;
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

    void SaveTiff(string filename)
    {
        using (var stream = new FileStream(filename, FileMode.Create))
        {
            var encoder = new TiffBitmapEncoder() { Compression = TiffCompressOption.Zip };
            BitmapFrame bmpFrame = BitmapFrame.Create(Image, null, GetTIFFMetadataD8M(), null);
            encoder.Frames.Add(bmpFrame);
            encoder.Save(stream);
        }
    }

    void SaveDng(string filename)
    {
        // Save RAW (two steps)
        // Step 1: Create a RAW TIFF file
        var filenameTiff = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "_raw.tiff");
        using (var stream = new FileStream(filenameTiff, FileMode.Create))
        {
            var encoder = new TiffBitmapEncoder() { Compression = TiffCompressOption.None };
            var frame = BitmapSource.Create(Image.PixelWidth, Image.PixelHeight, 96.0, 96.0, PixelFormats.Gray16, null, RawPixels, Image.PixelWidth * 2);
            BitmapFrame bmpFrame = BitmapFrame.Create(frame, null, GetTIFFMetadataD8M(), null);
            encoder.Frames.Add(bmpFrame);
            encoder.Save(stream);
        }
        // Step 2: Add DNG tags and save as .DNG
        // We use ExifTool https://exiftool.org because of BitmapMetadata limitations
        // The tool dng_validate.exe is available with the DNG SDK and can be used to validate the DNG file.
        // Please Note: The following metadata don't fully characterize the camera. Add your camera calibration data
        // (LinearizationTable,CalibrationIlluminant1,ColorMatrix1,etc..) to have a DNG profile that renders nice straight out of the box.
        // A Lens profile can also be built by using "Adobe Lens Profile Creator" or similar software to correct the geometry.
        string identityMatrix = "1.0 0.0 0.0 0.0 1.0 0.0 0.0 0.0 1.0";
        string dngMetadata =
            "-DNGVersion=1.3.0.0 " +
            "-EXIF:SubfileType=\"Full-resolution Image\" " +
            "-PhotometricInterpretation=\"Color Filter Array\" " +  // Bayer Pattern Image
            "-IFD0:CFARepeatPatternDim=\"2 2\" " +                  // Bayer Pattern Size: 2x2
            "-IFD0:CFAPattern2=\"2 1 1 0\" " +                      // Bayer Pattern: BGGR
            $"-ColorMatrix1=\"{identityMatrix}\" " +
            "-Orientation=Horizontal " +
            $"-UniqueCameraModel=\"{maker} {model}\" ";
        if (IsWhiteBalanceEnabled)
        {
            dngMetadata += $"-AnalogBalance=\"{MWBGainRed} {MWBGainGreen} {MWBGainBlue}\" ";
        }
        Process.Start(new ProcessStartInfo("exiftool.exe", $"{dngMetadata} -o \"{filename}\" \"{filenameTiff}\"") { CreateNoWindow = true }).WaitForExit();
        //File.Delete(filenameTiff);
    }

    [RelayCommand]
    public void Reset()
    {
        _sensor.Reset();
    }

    // Experimental function
    [RelayCommand]
    public async Task ExposureBracketing()
    {
        _sensor.WriteReg(0x3500, 0);
        _sensor.WriteReg(0x3501, 0);
        _sensor.WriteReg(0x3502, 0);
        for (byte i = 0; i < 255; i++)
        {
            _sensor.WriteReg(0x3502, (byte)(i));
            await Task.Delay(500);
            await DownloadImage();
            Image.SavePNG(i + ".png");
        }
    }

    // Experimental function
    [RelayCommand]
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