// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using CameraVision2;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraVision
{
    public partial class MainWindowVM : ObservableObject
    {
        OV8865 _sensor;
        WriteableBitmap _image;
        DemosaicingAlgorithms _currentDemosaicingAlgorithm;
        VideoSetting _currentVideoSetting;
        double _downloadProgress;
        Visibility _progressVisibility = Visibility.Collapsed;
        PointCollection _histogramPoints;
        PixelFormat _pixelFormat = PixelFormats.Rgba64;

        [ObservableProperty]
        bool _isEnabled;
        [ObservableProperty]
        int _minimumValue, _maximumValue;
        OV8865.COM _communication = OV8865.COM.COM_JTAG;

        public UInt16[] rawPixels;

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

        public ObservableCollection<Register> Registers { get; set; }
        public ObservableCollection<Register> MipiRegisters { get; set; }
        public ICommand DownloadImageCommand { get; set; }
        public ICommand SaveImageCommand { get; set; }
        public ICommand ReadRegistersCommand { get; set; }
        public ICommand ReadMipiRegistersCommand { get; set; }
        public ICommand LoadRegistersCommand { get; set; }
        public ICommand SaveRegistersCommand { get; set; }
        public ICommand FocusBracketingCommand { get; set; }
        public ICommand ExposureBracketingCommand { get; set; }
        public List<VideoSetting> VideoSettings { get; set; }
        public DemosaicingAlgorithms[] DemosaicingAlgorithms => (DemosaicingAlgorithms[])Enum.GetValues(typeof(CameraVision.DemosaicingAlgorithms));
        public OV8865.COM[] Communications => new OV8865.COM[] { OV8865.COM.COM_JTAG, OV8865.COM.COM_FT232H };

        public MainWindowVM()
        {
            Application.Current.DispatcherUnhandledException += (s, e) => MessageBox.Show(e.Exception.ToString());
        }

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
            // TODO: Read available settings from external file
            VideoSettings = new List<VideoSetting>()
            {
                new VideoSetting(){ Description="3264x2448 8MP", Registers=new List<Register>(){
                    new Register(0x3808,0x0C),
                    new Register(0x3809,0xC0),
                    new Register(0x380A,0x09),
                    new Register(0x380B,0x90),
                    new Register(0x3821,0x00),
                } },
                new VideoSetting(){ Description="1632x1224 2MP (2x2 binning)", Registers=new List<Register>(){
                    new Register(0x3808,0x06),
                    new Register(0x3809,0x60),
                    new Register(0x380A,0x04),
                    new Register(0x380B,0xC8),
                    new Register(0x3814,0x03),
                    new Register(0x3815,0x01),
                    new Register(0x382A,0x03),
                    new Register(0x382B,0x01),
                    new Register(0x3821,0x70),
                    new Register(0x3830,0x04),
                    new Register(0x3836,0x01),
                } },
                new VideoSetting(){ Description="1920x1080 2MP FHD", Registers=new List<Register>(){
                    new Register(0x3808,0x07),
                    new Register(0x3809,0x80),
                    new Register(0x380A,0x04),
                    new Register(0x380B,0x38),
                } },
                new VideoSetting(){ Description="1440x900 1.3MP WXGA+", Registers=new List<Register>(){
                    new Register(0x3808,0x05),
                    new Register(0x3809,0xA0),
                    new Register(0x380A,0x03),
                    new Register(0x380B,0x84),
                    //new Register(0x3821,0x48),
                } },
                new VideoSetting(){ Description="1280x1024 1.3MP", Registers=new List<Register>(){
                    new Register(0x3808,0x05),
                    new Register(0x3809,0x00),
                    new Register(0x380A,0x04),
                    new Register(0x380B,0x00),
                    //new Register(0x3821,0x48),
                } },
                new VideoSetting(){ Description="1366x768 1MP HD", Registers=new List<Register>(){
                    new Register(0x3808,0x05),
                    new Register(0x3809,0x56),
                    new Register(0x380A,0x03),
                    new Register(0x380B,0x00),
                } },
                new VideoSetting(){ Description="1024x768 0.7MP XGA", Registers=new List<Register>(){
                    new Register(0x3808,0x04),
                    new Register(0x3809,0x00),
                    new Register(0x380A,0x03),
                    new Register(0x380B,0x00),
                } },
                new VideoSetting(){ Description="816x612 0.5MP (4x4 skipping)", Registers=new List<Register>(){ // 7
                    new Register(0x3808,0x03),
                    new Register(0x3809,0x30),
                    new Register(0x380A,0x02),
                    new Register(0x380B,0x64),
                    new Register(0x3814,0x07),
                    new Register(0x3815,0x01),
                    new Register(0x382A,0x07),
                    new Register(0x382B,0x01),
                    new Register(0x3830,0x08),
                    new Register(0x3836,0x02),
                } },
                new VideoSetting(){ Description="800x600 SVGA (2x2 binning sum)", Registers=new List<Register>(){
                    new Register(0x3808,0x03),
                    new Register(0x3809,0x20),
                    new Register(0x380A,0x02),
                    new Register(0x380B,0x58),
                    new Register(0x3814,0x03),
                    new Register(0x3815,0x01),
                    new Register(0x382A,0x03),
                    new Register(0x382B,0x01),
                    new Register(0x3821,0xF1),
                } },
                new VideoSetting(){ Description="800x600 SVGA", Registers=new List<Register>(){ // 9
                    new Register(0x3808,0x03),
                    new Register(0x3809,0x20),
                    new Register(0x380A,0x02),
                    new Register(0x380B,0x58),
                } },
                new VideoSetting(){ Description="800x480", Registers=new List<Register>(){
                    new Register(0x3808,0x03),
                    new Register(0x3809,0x20),
                    new Register(0x380A,0x01),
                    new Register(0x380B,0xE0),
                } },
                new VideoSetting(){ Description="640x480 0.3MP VGA (4x4 skipping)", Registers=new List<Register>(){ // 11
                    new Register(0x3808,0x02),
                    new Register(0x3809,0x80),
                    new Register(0x380A,0x01),
                    new Register(0x380B,0xE0),
                    new Register(0x3814,0x07),
                    new Register(0x3815,0x01),
                    new Register(0x382A,0x07),
                    new Register(0x382B,0x01),
                    new Register(0x3830,0x08),
                    new Register(0x3836,0x02),
                } },
                new VideoSetting(){ Description="640x480 0.3MP VGA", Registers=new List<Register>(){
                    new Register(0x3808,0x02),
                    new Register(0x3809,0x80),
                    new Register(0x380A,0x01),
                    new Register(0x380B,0xE0),
                } },
                new VideoSetting(){ Description="320x240 CGA", Registers=new List<Register>(){
                    new Register(0x3808,0x01),
                    new Register(0x3809,0x40),
                    new Register(0x380A,0x00),
                    new Register(0x380B,0xF0),
                } },
                new VideoSetting(){ Description="160x120 QQVGA", Registers=new List<Register>(){
                    new Register(0x3808,0x00),
                    new Register(0x3809,0xA0),
                    new Register(0x380A,0x00),
                    new Register(0x380B,0x78),
                } }
            };
            CurrentDemosaicingAlgorithm = CameraVision.DemosaicingAlgorithms.SIMPLE_INTERPOLATION;
            CurrentVideoSetting = VideoSettings[11];

            DownloadImageCommand = new AsyncRelayCommand(DownloadImage);
            SaveImageCommand = new DelegateCommand(SaveImage);
            ReadRegistersCommand = new DelegateCommand(ReadRegisters);
            ReadMipiRegistersCommand = new DelegateCommand(ReadMipiRegisters);
            LoadRegistersCommand = new DelegateCommand(LoadRegisters);
            FocusBracketingCommand = new DelegateCommand(FocusBracketing);
            //SaveRegistersCommand = new DelegateCommand(SaveRegisters);
            ExposureBracketingCommand = new DelegateCommand(ExposureBracketing);
            HistogramPoints = new PointCollection();
            Registers = new ObservableCollection<Register>();
            MipiRegisters = new ObservableCollection<Register>();
            //await Task.Delay(250);
            //_sensor.WriteReg(0x5E00, 0x80); // Color Bar
            //_sensor.WriteReg(0x5E00, 0x92); // Square Color Bar            
        }

        // Experimental function
        async void ExposureBracketing()
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
        async void FocusBracketing()
        {
            Focus = 1000;
            while (Focus > 500)
            {
                await DownloadImage();
                Image.SavePNG(Focus + ".png");
                Focus = Focus - 10;
            }
        }

        /// <summary>
        /// Load Registers from custom .ovr file format
        /// </summary>
        public void LoadRegisters()
        {
            // TODO: Reset first the current registers?
            var dlg = new OpenFileDialog();
            dlg.FileName = "ov8865";
            dlg.DefaultExt = ".ovr";
            dlg.Filter = "OV8865 registers (.ovr)|*.ovr";
            var res = dlg.ShowDialog();
            if (res == true)
            {
                string filename = dlg.FileName;
                string[] lines = File.ReadAllLines(filename);
                foreach (var line in lines)
                {
                    var tokens = line.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    UInt16 addr = UInt16.Parse(tokens[0]);
                    byte val = byte.Parse(tokens[1]);
                    _sensor.WriteReg(addr, val);
                }
            }
        }

        void AddRegister(UInt16 addr, string desc = "")
        {
            var reg = new Register(addr, _sensor.ReadReg(addr).ToString("X"), desc);
            reg.PropertyChanged += Reg_PropertyChanged;
            Registers.Add(reg);
        }

        void Reg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var reg = sender as Register;
            _sensor.WriteReg(reg.Address, Convert.ToByte(reg.Value, 16));
        }

        void AddMipiRegister(UInt16 addr, string desc = "")
        {
            var reg = new Register(addr, _sensor.ReadRegMipi(addr).ToString("X"), desc);
            reg.PropertyChanged += MipiReg_PropertyChanged;
            MipiRegisters.Add(reg);
        }

        void MipiReg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var reg = sender as Register;
            _sensor.WriteRegMipi(reg.Address, Convert.ToUInt16(reg.Value, 16));
        }

        public void ReadRegisters()
        {
            Registers.Clear();

            // TODO: Read registers descriptions from extenal file

            //AddRegister(0x0100, "OV8865_SC_CTRL0100");
            //AddRegister(0x0103, "OV8865_SC_CTRL0103");

            // PLL
            //AddRegister(0x0300, "PLL_CTRL_0");
            //AddRegister(0x0301, "PLL_CTRL_1");
            //AddRegister(0x0302, "PLL_CTRL_2");
            //AddRegister(0x0303, "PLL_CTRL_3");
            //AddRegister(0x0304, "PLL_CTRL_4");
            //AddRegister(0x0305, "PLL_CTRL_5");
            //AddRegister(0x0306, "PLL_CTRL_6");
            //AddRegister(0x0307, "PLL_CTRL_7");
            //AddRegister(0x0308, "PLL_CTRL_8");
            //AddRegister(0x0309, "PLL_CTRL_9");
            //AddRegister(0x030A, "PLL_CTRL_A");
            //AddRegister(0x030B, "PLL_CTRL_B");
            //AddRegister(0x030C, "PLL_CTRL_C");
            //AddRegister(0x030D, "PLL_CTRL_D");
            //AddRegister(0x030E, "PLL_CTRL_E");
            //AddRegister(0x030F, "PLL_CTRL_F");
            //AddRegister(0x0310, "PLL_CTRL_10");
            //AddRegister(0x0311, "PLL_CTRL_11");
            //AddRegister(0x0312, "PLL_CTRL_12");
            //AddRegister(0x031B, "PLL_CTRL_1B");
            //AddRegister(0x031C, "PLL_CTRL_1C");
            //AddRegister(0x031E, "PLL_CTRL_1E");
            //AddRegister(0x3106, "SCLK_DIV, SCLK_PRE_DIV");
            //AddRegister(0x3007, "R ISPOUT BITSEL");
            //AddRegister(0x3011, "PAD");
            //AddRegister(0x3020, "PCLK_DIV");
            //AddRegister(0x3032, "MUX PLL_SYS_CLK");
            //AddRegister(0x3033, "MUX DAC_SYS_CLK");

            //AddRegister(0x3031, "REG31");   // MIPI BIT SEL

            // Image Windowing Control
            AddRegister(0x3808, "H_OUTPUT_SIZE 11:8");
            AddRegister(0x3809, "H_OUTPUT_SIZE 7:0");
            AddRegister(0x380A, "V_OUTPUT_SIZE 11:8");
            AddRegister(0x380B, "V_OUTPUT_SIZE 7:0");

            AddRegister(0x380C, "TIMING_HTS 15:8");
            AddRegister(0x380D, "TIMING_HTS 7:0");
            AddRegister(0x380E, "TIMING_VTS 15:8");
            AddRegister(0x380F, "TIMING_VTS 7:0");

            //AddRegister(0x3810, "H_WIN_OFF 15:8");
            //AddRegister(0x3811, "H_WIN_OFF 7:0");
            //AddRegister(0x3812, "V_WIN_OFF 11:8");
            //AddRegister(0x3813, "V_WIN_OFF 7:0");

            AddRegister(0x3842, "H_AUTO_OFF_H");
            AddRegister(0x3843, "H_AUTO_OFF_L");
            AddRegister(0x3844, "V_AUTO_OFF_H");
            AddRegister(0x3845, "V_AUTO_OFF_L");


            AddRegister(0x3820, "TIMING_FORMAT1");

            // Binning
            AddRegister(0x3821, "TIMING_FORMAT2");
            AddRegister(0x3814, "X_ODD_INC");
            AddRegister(0x3815, "X_EVEN_INC");
            AddRegister(0x382A, "Y_ODD_INC");
            AddRegister(0x382B, "Y_EVEN_INC");

            // DSP top
            AddRegister(0x5000, "DSP CTRL00");
            AddRegister(0x5001, "DSP CTRL01");  // BLC function enable
            AddRegister(0x5002, "DSP CTRL02");  // Variopixel function enable
            AddRegister(0x5003, "DSP CTRL03");
            AddRegister(0x5004, "DSP CTRL04");
            AddRegister(0x5005, "DSP CTRL05");
            AddRegister(0x501F, "DSP CTRL1F");
            AddRegister(0x5025, "DSP CTRL25");
            AddRegister(0x5041, "DSP CTRL41");
            AddRegister(0x5043, "DSP CTRL43");

            // Pre DSP (Test Pattern Registers)
            AddRegister(0x5E00, "PRE CTRL00");  // Color Bar = 0x84
            AddRegister(0x5E01, "PRE CTRL01");

            // Defective Pixel Cancellation (DPC)
            AddRegister(0x5000, "ISP CTRL00");

            // Window Cut (WINC)
            //AddRegister(0x5A00, "WINC CTRL00");
            //AddRegister(0x5A01, "WINC CTRL01");
            //AddRegister(0x5A02, "WINC CTRL02");
            //AddRegister(0x5A03, "WINC CTRL03");
            //AddRegister(0x5A04, "WINC CTRL04");
            //AddRegister(0x5A05, "WINC CTRL05");
            //AddRegister(0x5A06, "WINC CTRL06");
            //AddRegister(0x5A07, "WINC CTRL07");
            //AddRegister(0x5A08, "WINC CTRL08");

            // Manual White Balance (MWB)
            AddRegister(0x5018, "ISP CTRL18");
            AddRegister(0x5019, "ISP CTRL19");
            AddRegister(0x501A, "ISP CTRL1A");
            AddRegister(0x501B, "ISP CTRL1B");
            AddRegister(0x501C, "ISP CTRL1C");
            AddRegister(0x501D, "ISP CTRL1D");
            AddRegister(0x501E, "ISP CTRL1E");

            // Manual Exposure Compensation (MEC) / Manual Gain Compensation (MGC)
            AddRegister(0x3500, "AEC EXPO 19:16");
            AddRegister(0x3501, "AEC EXPO 15:8");
            AddRegister(0x3502, "AEC EXPO 7:0");
            AddRegister(0x3503, "AEC MANUAL");
            //AddRegister(0x3505, "GCVT OPTION");
            //AddRegister(0x3507, "AEC GAIN SHIFT");
            AddRegister(0x3508, "AEC GAIN 12:8");
            AddRegister(0x3509, "AEC GAIN 7:0");
            AddRegister(0x350A, "AEC DIGIGAIN 13:6");
            AddRegister(0x350B, "AEC DIGIGAIN 5:0");

            // System Control
            //AddRegister(0x300A, "CHIP ID 23:16");
            //AddRegister(0x300B, "CHIP ID 15:8");
            //AddRegister(0x300C, "CHIP ID 7:0");

            // Timing Control Registers
            //AddRegister(0x3822, "REG22");
            //AddRegister(0x382C, "BLC COL ST L");
            //AddRegister(0x382D, "BLC COL END L");
            //AddRegister(0x382E, "BLC COL ST R");
            //AddRegister(0x382F, "BLC COL END R");
            //AddRegister(0x3830, "BLC NUM OPTION");
            //AddRegister(0x3831, "BLC NUM MAN");
            //AddRegister(0x3836, "ZLINE NUM OPTION");

            // BLC Control
            //AddRegister(0x4000, "BLC CTRL00");
            AddRegister(0x4004, "BLC CTRL04 target 15:8");
            AddRegister(0x4005, "BLC CTRL05 target 7:0");

            // Format Control
            AddRegister(0x4300, "CLIP MAX HI");
            AddRegister(0x4301, "CLIP MIN HI");
            //AddRegister(0x4302, "CLIP LO");
            //AddRegister(0x4303, "FORMAT CTRL3");
            //AddRegister(0x4304, "FORMAT CTRL4");

            //AddRegister(0x4308, "TEST X START HIGH");
            //AddRegister(0x4309, "TEST X START LOW");
            //AddRegister(0x430A, "TEST Y START HIGH");
            //AddRegister(0x430B, "TEST Y START LOW");
            //AddRegister(0x430C, "TEST WIDTH HIGH");
            //AddRegister(0x430D, "TEST WIDTH LOW");
            //AddRegister(0x430E, "TEST HEIGHT HIGH");
            //AddRegister(0x430F, "TEST HEIGHT LOW");

            AddRegister(0x4320, "TEST PATTERN CTRL");
            AddRegister(0x4322, "SOLID COLOR B");
            AddRegister(0x4323, "SOLID COLOR B");
            AddRegister(0x4324, "SOLID COLOR GB");
            AddRegister(0x4325, "SOLID COLOR GB");
            AddRegister(0x4326, "SOLID COLOR R");
            AddRegister(0x4327, "SOLID COLOR R");
            AddRegister(0x4328, "SOLID COLOR GR");
            AddRegister(0x4329, "SOLID COLOR GR");

            // MIPI
            //AddRegister(0x4801, "MIPI CTRL01");
            //AddRegister(0x4802, "MIPI CTRL02");
            //AddRegister(0x4803, "MIPI CTRL03");
            //AddRegister(0x4804, "MIPI CTRL04");
            //AddRegister(0x4805, "MIPI CTRL05");
            //AddRegister(0x4806, "MIPI CTRL06");
            //AddRegister(0x4807, "MIPI CTRL07");
            //AddRegister(0x4808, "MIPI CTRL08");
            //AddRegister(0x4813, "MIPI CTRL013");
            //AddRegister(0x4814, "MIPI CTRL014");
            //AddRegister(0x4815, "MIPI CTRL015");

            // OTP
            // AddRegister(0x3D81, "OTP_REG85");

            // Update GUI elements
            OnPropertyChanged("FPS");
            OnPropertyChanged("ExposureMs"); OnPropertyChanged("Exposure");
            OnPropertyChanged("AnalogGain"); OnPropertyChanged("ISO");
            OnPropertyChanged("IsWhiteBalanceEnabled");
            OnPropertyChanged("MWBGainRed"); OnPropertyChanged("MWBGainGreen"); OnPropertyChanged("MWBGainBlue");
        }

        public void ReadMipiRegisters()
        {
            MipiRegisters.Clear();
            AddMipiRegister(0x0000, "ChipID");
            AddMipiRegister(0x0002, "SysCtl");
            AddMipiRegister(0x0004, "ConfCtl");
            AddMipiRegister(0x0006, "FiFoCtl");
            AddMipiRegister(0x0008, "DataFmt");
            AddMipiRegister(0x000C, "MclkCtl");
            AddMipiRegister(0x0016, "PLLCtl0");
            AddMipiRegister(0x0018, "PLLCtl1");
            AddMipiRegister(0x0020, "CLKCtrl");
            AddMipiRegister(0x0022, "WordCnt");

            AddMipiRegister(0x0060, "PHYTimDly");


            AddMipiRegister(0x0064, "CSIStatus");
            AddMipiRegister(0x006A, "CSIDID");

            // CSI2-RX Status Counters
            AddMipiRegister(0x0080, "FrmErrCnt");
            AddMipiRegister(0x0082, "CRCErrCnt");
            AddMipiRegister(0x0084, "CorErrCnt");
            AddMipiRegister(0x0086, "HdrErrCnt");
            AddMipiRegister(0x0088, "EIDErrCnt");
            AddMipiRegister(0x008A, "CtlErrCnt");
            AddMipiRegister(0x008C, "SotErrCnt");
            AddMipiRegister(0x008E, "SynErrCnt");
            AddMipiRegister(0x0090, "MDLErrCnt");
            AddMipiRegister(0x00F8, "FIFOStatus");

            // Debug Tx
            //AddMipiRegister(0x00e0, "DBG_LCNT");
            //AddMipiRegister(0x00e2, "DBG_WIDTH");
            //AddMipiRegister(0x00e4, "DBG_VBlank");
            //AddMipiRegister(0x00e8, "DBG_Data");
        }

        public double Exposure
        {
            get
            {
                return (double)(_sensor.GetExposure());
            }
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
                OnPropertyChanged("ExposureMs");
            }
        }

        public double Focus
        {
            get { return (double)(_sensor.GetFocus()); }
            set { _sensor.SetFocus((UInt16)value); OnPropertyChanged(); }
        }

        public double AnalogGain
        {
            get { return (double)(_sensor.GetAnalogGain() + 1); }
            set { _sensor.SetAnalogGain((byte)(value - 1)); OnPropertyChanged(); OnPropertyChanged("ISO"); }
        }

        public double ISO
        {
            get { return AnalogGain * 100; }
        }

        public double MWBGainRed
        {
            get { return (double)(_sensor.GetMWBGainRed() / 1024); }
            set { _sensor.SetMWBGainRed((UInt16)(value * 1024)); OnPropertyChanged(); }
        }

        public double MWBGainGreen
        {
            get { return (double)(_sensor.GetMWBGainGreen() / 1024); }
            set { _sensor.SetMWBGainGreen((UInt16)(value * 1024)); OnPropertyChanged(); }
        }

        public double MWBGainBlue
        {
            get { return (double)(_sensor.GetMWBGainBlue() / 1024); }
            set { _sensor.SetMWBGainBlue((UInt16)(value * 1024)); OnPropertyChanged(); }
        }

        public WriteableBitmap Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged();
            }
        }

        public double DownloadProgress
        {
            get { return _downloadProgress; }
            set
            {
                _downloadProgress = value; OnPropertyChanged();
            }
        }

        public Visibility ProgressVisibility
        {
            get { return _progressVisibility; }
            set
            {
                _progressVisibility = value; OnPropertyChanged();
            }
        }

        public VideoSetting CurrentVideoSetting
        {
            get
            {
                return _currentVideoSetting;
            }
            set
            {
                _currentVideoSetting = value;
                OnPropertyChanged();

                //_sensor.WriteReg(0x0100, 0x00); // Software Standby                

                // No skipping or binning                

                _sensor.WriteReg(0x3814, 0x01);
                Thread.Sleep(200);
                _sensor.WriteReg(0x3815, 0x01);
                Thread.Sleep(200);
                _sensor.WriteReg(0x382A, 0x01);
                Thread.Sleep(200);
                _sensor.WriteReg(0x382B, 0x01);
                Thread.Sleep(200);

                _sensor.WriteReg(0x3821, 0x40); // FORMAT2 = hsync_en_o
                _sensor.WriteReg(0x3830, 0x08); // BLC NUM OPTION
                _sensor.WriteReg(0x3836, 0x02); // ZLINE NUM OPTION

                //_sensor.WriteReg(0x0100, 0x01); // Streaming

                foreach (var reg in _currentVideoSetting.Registers)
                {
                    _sensor.WriteReg(reg.Address, byte.Parse(reg.Value, System.Globalization.NumberStyles.HexNumber));
                    if ((reg.Address == 0x3814) | (reg.Address == 0x3815) | (reg.Address == 0x382A) | (reg.Address == 0x382B))
                    {
                        Thread.Sleep(200);
                    }
                }

                int ImageWidthH = int.Parse(_currentVideoSetting.Registers.Where((r) => r.Address == 0x3808).First().Value, System.Globalization.NumberStyles.HexNumber);
                int ImageWidthL = int.Parse(_currentVideoSetting.Registers.Where((r) => r.Address == 0x3809).First().Value, System.Globalization.NumberStyles.HexNumber);
                int ImageWidth = (ImageWidthH << 8) | ImageWidthL;
                int ImageHeightH = int.Parse(_currentVideoSetting.Registers.Where((r) => r.Address == 0x380A).First().Value, System.Globalization.NumberStyles.HexNumber);
                int ImageHeightL = int.Parse(_currentVideoSetting.Registers.Where((r) => r.Address == 0x380B).First().Value, System.Globalization.NumberStyles.HexNumber);
                int ImageHeight = (ImageHeightH << 8) | ImageHeightL;
                _sensor.Config((ushort)ImageWidth, (ushort)ImageHeight);

                Image = new WriteableBitmap(ImageWidth, ImageHeight, 96, 96, _pixelFormat, null);
            }
        }

        public DemosaicingAlgorithms CurrentDemosaicingAlgorithm
        {
            get
            {
                return _currentDemosaicingAlgorithm;
            }
            set
            {
                _currentDemosaicingAlgorithm = value;
                OnPropertyChanged();
            }
        }

        public bool IsWhiteBalanceEnabled
        {
            get
            {
                return _sensor.GetWhiteBalanceEnable();
            }
            set
            {
                _sensor.SetWhiteBalanceEnable(value);
                OnPropertyChanged();
            }
        }

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

        public PointCollection HistogramPoints
        {
            get
            {
                return _histogramPoints;
            }
            set
            {
                _histogramPoints = value;
                OnPropertyChanged();
            }
        }

        private static double Saturate(double val, double min, double max)
        {
            if (val > max)
                return max;
            if (val < min)
                return min;
            return val;
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

        public async Task DownloadImage()
        {
            try
            {
                IsEnabled = false;
                int imageWidth = (int)Image.Width;
                int imageHeight = (int)Image.Height;
                rawPixels = await _sensor.GetImage(imageWidth, imageHeight, new Progress<double>(UpdateProgress));
                Debug.WriteLine("Frame received");
                MinimumValue = rawPixels.Min();
                MaximumValue = rawPixels.Max();
                CreateHistogram(rawPixels);
                ulong[] colorPixels = null;
                switch (CurrentDemosaicingAlgorithm)
                {
                    case CameraVision.DemosaicingAlgorithms.SIMPLE_INTERPOLATION:
                        colorPixels = BayerAlgoritms.Demosaic(rawPixels, imageWidth, imageHeight);
                        break;
                    case CameraVision.DemosaicingAlgorithms.BGGR_BAYER_RAW:
                        colorPixels = BayerAlgoritms.ColorRaw(rawPixels, imageWidth, imageHeight);
                        break;
                    case CameraVision.DemosaicingAlgorithms.GRAY16_RAW:
                        colorPixels = BayerAlgoritms.GrayRaw(rawPixels, imageWidth, imageHeight);
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

        BitmapMetadata GetTIFFMetadataD8M()
        {
            // D8M / OV8865 specs
            const double f = 3.37;
            const double fstop = 2.8;
            const double sensorWidth = 4.6144;
            const double sensorHeight = 3.472;
            const string maker = "";
            const string model = "D8M";
            double sensor35Diagonal = Math.Sqrt(36 * 36 + 24 * 24);
            double sensorDiagonal = Math.Sqrt(sensorWidth * sensorWidth + sensorHeight * sensorHeight);
            double cropFactor = sensor35Diagonal / sensorDiagonal;
            double focalLengthIn35mmFilm = cropFactor * f;
            // Metadata query documentation: https://learn.microsoft.com/en-us/windows/win32/wic/system-photo
            var bmpMetadata = new BitmapMetadata("tiff");
            // TIFF Exif metadata
            bmpMetadata.SetQuery("/ifd/exif/{ushort=37386}", f);
            bmpMetadata.SetQuery("/ifd/exif/{ushort=41989}", focalLengthIn35mmFilm.ToString());
            bmpMetadata.SetQuery("/ifd/exif/{ushort=33437}", fstop);
            bmpMetadata.SetQuery("/ifd/{ushort=271}", maker);
            bmpMetadata.SetQuery("/ifd/{ushort=272}", model);
            // TIFF XMP metadata
            bmpMetadata.SetQuery("/ifd/xmp/exif:FocalLength", f.ToString());
            bmpMetadata.SetQuery("/ifd/xmp/exif:FocalLengthIn35mmFilm", focalLengthIn35mmFilm.ToString());
            bmpMetadata.SetQuery("/ifd/xmp/exif:FNumber", fstop.ToString());
            bmpMetadata.SetQuery("/ifd/xmp/tiff:Make", maker);
            bmpMetadata.SetQuery("/ifd/xmp/tiff:Model", model);
            return bmpMetadata;
        }

        public void SaveImage()
        {
            var dlg = new SaveFileDialog();
            dlg.FileName = "camera_" + DateTime.Now.ToString("hhmmss") + ".tiff";
            dlg.DefaultExt = ".tiff";
            dlg.Filter = "TIFF image (.tiff)|*.tiff";
            var res = dlg.ShowDialog();
            if (res == true)
            {
                // Save Color
                string filename = dlg.FileName;
                using (var stream = new FileStream(filename, FileMode.Create))
                {
                    var encoder = new TiffBitmapEncoder() { Compression = TiffCompressOption.None };
                    BitmapFrame bmpFrame = BitmapFrame.Create(Image, null, GetTIFFMetadataD8M(), null);
                    encoder.Frames.Add(bmpFrame);
                    encoder.Save(stream);
                }
                // Save RAW
                filename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "_raw.tiff");
                using (var stream = new FileStream(filename, FileMode.Create))
                {
                    var encoder = new TiffBitmapEncoder() { Compression = TiffCompressOption.None };
                    BitmapFrame bmpFrame = BitmapFrame.Create(BitmapSource.Create(Image.PixelWidth, Image.PixelHeight, 96.0, 96.0, PixelFormats.Gray16, null, rawPixels, Image.PixelWidth * 2));
                    encoder.Frames.Add(bmpFrame);
                    encoder.Save(stream);
                }
            }
        }
    }
}