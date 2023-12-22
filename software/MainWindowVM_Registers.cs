// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace CameraVision;

public partial class MainWindowVM
{
    [ObservableProperty]
    ObservableCollection<Register> _registers = new ObservableCollection<Register>();
    [ObservableProperty]
    ObservableCollection<Register> _mipiRegisters = new ObservableCollection<Register>();

    public OV8865.COM Communication
    {
        get => IsConnected ? _sensor.ReadCom() : OV8865.COM.COM_NONE;
        set
        {
            if (IsConnected)
            {
                _sensor.WriteCom(value);
                _sensor.SyncCom();
                OnPropertyChanged();
            }
        }
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

    public bool IsLensCorrectionEnabled
    {
        get => IsConnected ? _sensor.GetLensCorrectionEnable() : false;
        set
        {
            if (!IsConnected)
                return;

            _sensor.SetLensCorrectionEnable(value);
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
}