// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

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
    bool _isEnabled;
    [ObservableProperty]
    int _minimumValue, _maximumValue;
    [ObservableProperty]
    UInt16[] _rawPixels;
    [ObservableProperty]
    bool _isRawLinearized = false;
    [ObservableProperty]
    bool _isConnected;

    OV8865.COM _communication = OV8865.COM.COM_JTAG;

    public OV8865.COM Communication
    {
        get => IsConnected ? _sensor.ReadCom() : OV8865.COM.COM_NONE;
        set
        {
            if (SetProperty(ref _communication, value))
            {
                if (IsConnected)
                {
                    _sensor.WriteCom(_communication);
                    _sensor.SyncCom();
                }
            }
        }
    }

    [ObservableProperty]
    ObservableCollection<Register> _registers = new ObservableCollection<Register>();
    [ObservableProperty]
    ObservableCollection<Register> _mipiRegisters = new ObservableCollection<Register>();

    public List<VideoSetting> VideoSettings { get; set; }
    public DemosaicingAlgorithms[] DemosaicingAlgorithms => (DemosaicingAlgorithms[])Enum.GetValues(typeof(DemosaicingAlgorithms));
    public OV8865.COM[] Communications => new OV8865.COM[] { OV8865.COM.COM_JTAG, OV8865.COM.COM_FT232H, OV8865.COM.COM_NONE };

    public void Initialize()
    {
        try
        {
            _sensor = new OV8865();

            Communication = OV8865.COM.COM_JTAG;
            VideoSettings = JsonSerializer.Deserialize<List<VideoSetting>>(File.ReadAllText(@"Settings\CameraSettings.json"));
            Thread.Sleep(250);
            // Hardcoded default settings
            CurrentVideoSetting = VideoSettings[6];
            Focus = 128;
            IsWhiteBalanceEnabled = true;
            MWBGainBlue = MWBGainGreen = MWBGainRed = 1.0;
            Communication = OV8865.COM.COM_FT232H;
            ReadRegisters();
            //_vm.ReadMipiRegisters();
            // Check Camera with a Color Bar
            //await Task.Delay(250);
            //_sensor.WriteReg(0x5E00, 0x80); // Color Bar
            //_sensor.WriteReg(0x5E00, 0x92); // Square Color Bar            
            IsConnected = true;
        }
        catch (Exception ex)
        {
            Communication = OV8865.COM.COM_NONE;
            IsConnected = false;
            //MessageBox.Show(ex.Message + "\nFPGA with D8M and the bitstream loaded is connected?");
            //Environment.Exit(-1);
        }
        IsEnabled = true;
    }

    public double Exposure
    {
        get => IsConnected ? (double)(_sensor.GetExposure()) : double.NaN;
        set
        {
            if (!IsConnected)
                return;

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
        get => IsConnected ? (double)(_sensor.GetFocus()) : double.NaN;
        set
        {
            if (!IsConnected)
                return;
            _sensor.SetFocus((UInt16)value);
            OnPropertyChanged();
        }
    }

    public double AnalogGain
    {
        get => IsConnected ? (double)(_sensor.GetAnalogGain() + 1) : double.NaN;
        set
        {
            if (!IsConnected)
                return;
            _sensor.SetAnalogGain((byte)(value - 1));
            OnPropertyChanged();
            OnPropertyChanged(nameof(ISO));
        }
    }

    public double ISO => IsConnected ? AnalogGain * 100 : double.NaN;

    public double MWBGainRed
    {
        get => IsConnected ? (double)(_sensor.GetMWBGainRed() / 1024) : double.NaN;
        set
        {
            if (!IsConnected)
                return;
            _sensor.SetMWBGainRed((UInt16)(value * 1024));
            OnPropertyChanged();
        }
    }

    public double MWBGainGreen
    {
        get => IsConnected ? (double)(_sensor.GetMWBGainGreen() / 1024) : double.NaN;
        set
        {
            if (!IsConnected)
                return;
            _sensor.SetMWBGainGreen((UInt16)(value * 1024));
            OnPropertyChanged();
        }
    }

    public double MWBGainBlue
    {
        get => IsConnected ? (double)(_sensor.GetMWBGainBlue() / 1024) : double.NaN;
        set
        {
            if (!IsConnected)
                return;
            _sensor.SetMWBGainBlue((UInt16)(value * 1024));
            OnPropertyChanged();
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
        }
    }
    public bool IsWhiteBalanceEnabled
    {
        get => IsConnected ? _sensor.GetWhiteBalanceEnable() : false;
        set
        {
            if (!IsConnected)
                return;

            _sensor.SetWhiteBalanceEnable(value);
            OnPropertyChanged();
        }
    }
    // For optimal performance exposure should be <200ms
    public double ExposureMs
    {
        get
        {
            if (!IsConnected)
                return double.NaN;

            UInt16 hts = _sensor.GetHTS();  // extra lines
            return Math.Round((1.0 / 125000.0) * (double)(hts) * (Exposure), 2);
        }
    }
    public double FPS
    {
        get
        {
            if (!IsConnected)
                return double.NaN;

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
            }; ;
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
        RawPixels = rawPixels;
        Debug.WriteLine("Frame received");
        int imageWidth = Image.PixelWidth;
        int imageHeight = Image.PixelHeight;
        if (IsRawLinearized)
        {
            RawPixels = Linearization.Linearize(RawPixels, imageWidth, imageHeight);
            Debug.WriteLine("Raw pixels linearized");
        }
        MinimumValue = RawPixels.Min();
        MaximumValue = RawPixels.Max();
        //CreateHistogram(RawPixels);
        ulong[] colorPixels = null;
        switch (CurrentDemosaicingAlgorithm)
        {
            case Algoritms.DemosaicingAlgorithms.SIMPLE_INTERPOLATION:
                colorPixels = Bayer.Demosaic(RawPixels, imageWidth, imageHeight);
                break;
            case Algoritms.DemosaicingAlgorithms.BGGR_BAYER_RAW:
                colorPixels = Bayer.ColorRaw(RawPixels, imageWidth, imageHeight);
                break;
            case Algoritms.DemosaicingAlgorithms.GRAY16_RAW:
                colorPixels = Bayer.GrayRaw(RawPixels, imageWidth, imageHeight);
                break;
            default:
                throw new Exception("Demosaicing algorithm not implemented");
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
        // https://exiftool.org/TagNames/EXIF.html#LightSource

        string CalibrationIlluminant1 = "Tungsten (Incandescent)";
        string ColorMatrix1 = "1.6572 -0.5447 -0.0647 0.4383 0.1854 0.1173 0.0805 0.0367 0.3337";

        string CalibrationIlluminant2 = "D50";
        string ColorMatrix2 = "1.3744 -0.4354 0.1334 0.1255 0.6122 0.3058 0.0622 0.0897 0.4731";

        string CalibrationIlluminant3 = "Warm White Fluorescent";   // CCT 2566K
        string ColorMatrix3 = "1.4365 -0.513 0.0944 0.0585 0.7667 0.2145 0.0607 0.0998 0.4381";

        string AsShotNeutral = "0.5439 1.0000 0.5965";  // RGB White Balance for D50

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
        var exifProcess = Process.Start(new ProcessStartInfo("exiftool.exe", $"{dngMetadata} -o \"{filename}\" \"{filenameTiff}\"")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        exifProcess.WaitForExit();
        Debug.Write($"ExifTool Output: {exifProcess.StandardOutput.ReadToEnd()}");
        Debug.Write($"ExifTool Error: {exifProcess.StandardError.ReadToEnd()}");
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

            RawPixels = new ushort[w * h * 2];
            frame.CopyPixels(RawPixels, w * 2, 0);
            Image = new WriteableBitmap(w, h, 96, 96, _pixelFormat, null);
            UpdateImage(RawPixels);
        }
    }
    void DebugBGGR(int x, int y)
    {    
        // x,y = B coordinates
        DebugPixel(x, y); Debug.Write(";");
        DebugPixel(x + 1, y); Debug.Write(";");
        DebugPixel(x, y + 1); Debug.Write(";");
        DebugPixel(x + 1, y + 1); Debug.Write(";\n");
    }
    void DebugPixel(int x, int y) => Debug.Write(RawPixels[y * Image.PixelWidth + x]);
    
    [RelayCommand(CanExecute = nameof(IsConnected))]
    public void Reset()
    {
        _sensor.Reset();
        Application.Current.Shutdown();
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