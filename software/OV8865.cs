// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace CameraVision;

/// <summary>
/// Represent OV8865 Image Sensor
/// </summary>
public class OV8865
{
    const byte CMD_MIPI_RD_REG = 0x08;
    const byte CMD_MIPI_WR_REG = 0x09;
    const byte CMD_READ = 0x01;
    const byte CMD_WRITE = 0x02;
    const byte CMD_WR_FOCUS = 0x03;
    const byte CMD_RD_FOCUS = 0x07;
    const byte CMD_RESET = 0x04;
    const byte CMD_CONFIG = 0x05;
    const byte CMD_RD_IMG = 0x06;
    const byte CMD_RD_COM = 0x10;
    const byte CMD_WR_COM = 0x11;

    public enum COM
    {
        [Description("JTAG")]
        COM_JTAG = 0,
        [Description("JTAG+FT232H")]
        COM_FT232H = 1,
        [Description("NONE")]
        COM_NONE = -1
    }
    COM _com;
    public StdIO _io, _fastIO;

    public OV8865()
    {
        _com = COM.COM_JTAG;
        _io = _fastIO = new JtagUart();
    }

    public void SyncCom()
    {
        switch (_com)
        {
            case COM.COM_JTAG:
                _fastIO = _io;
                break;
            case COM.COM_FT232H:
                _fastIO = new FT232H_IO();
                break;
            default:
                break;
        }
    }

    public COM ReadCom()
    {
        _io.WriteByte(CMD_RD_COM);
        var com = (COM)_io.ReadByte();
        CheckResponseOk();
        Debug.WriteLine($"ReadCom returned {com}");
        _com = com;
        return com;
    }
    public void WriteCom(COM com)
    {
        Debug.WriteLine($"WriteCom {com}");
        _io.WriteByte(CMD_WR_COM);
        _io.WriteByte((byte)com);
        CheckResponseOk();
        _com = com;
    }

    public byte ReadReg(UInt16 addr)
    {
        _io.WriteByte(CMD_READ);
        _io.WriteUInt16(addr);
        byte val = _io.ReadByte();
        Debug.WriteLine("ReadReg " + addr.ToString("X") + ":" + val.ToString("X"));
        return val;
    }

    public void WriteReg(UInt16 addr, byte val)
    {
        Debug.WriteLine("WriteReg " + addr.ToString("X") + ":" + val.ToString("X"));
        _io.WriteByte(CMD_WRITE);
        _io.WriteUInt16(addr);
        _io.WriteByte(val);
    }

    public UInt16 ReadRegMipi(UInt16 addr)
    {
        _io.WriteByte(CMD_MIPI_RD_REG);
        _io.WriteUInt16(addr);
        UInt16 val = _io.ReadUInt16();
        Debug.WriteLine("ReadRegMipi " + addr.ToString("X") + ":" + val.ToString("X"));
        return val;
    }

    public void WriteRegMipi(UInt16 addr, UInt16 val)
    {
        Debug.WriteLine("WriteRegMipi " + addr.ToString("X") + ":" + val.ToString("X"));
        _io.WriteByte(CMD_MIPI_WR_REG);
        _io.WriteUInt16(addr);
        _io.WriteUInt16(val);
    }

    public void Reset()
    {
        Debug.WriteLine("Sensor Reset");
        _io.WriteByte(CMD_RESET);
        CheckResponseOk();
    }

    public void Config(UInt16 image_width, UInt16 image_height)
    {
        _io.WriteByte(CMD_CONFIG);
        _io.WriteUInt16(image_width);
        _io.WriteUInt16(image_height);
        CheckResponseOk();
    }

    /// <summary>
    /// Set exposure in units of 1/16 line.
    /// For optimal performance, maximum exposure should be 200ms.
    /// Maximum exposure interval: 2480 x Trow
    /// input range: 0-512.
    /// 
    /// Exposure could be increased further by insert dummy lines (change also frame rate)
    /// Dummy lines can be before (extra line) and after (dummy line) data output
    /// 
    /// Extra line increase actual exposure time
    /// Exposure = Shutter + Extra Lines
    /// 
    /// Dummy lines increase maximum shutter time
    /// 
    /// Maximum exposure = VTS - 4
    /// 
    /// The actual exposure time can be calculated by the equation:    
    /// (1 / System Frequency ) * (Tline) * (Exposure Time in this setting)
    /// (1 / 125.000.000 ) * ( HTS ) * ( exposure )
    /// 
    /// Banding filter for 50 Hz:    
    /// (1/100)/(HTS*SysClk)= SysClk/(HTS*100)
    /// 
    /// </summary>
    public void SetExposure(UInt16 exposure)
    {
        // Exposure: 20 bit, low 4 bits are fraction bits (ignored)
        byte b7_0, b15_8, b19_16;
        b19_16 = (byte)((exposure & 0xF000) >> 12);
        b15_8 = (byte)((exposure & 0x0FF0) >> 4);
        b7_0 = (byte)(((exposure & 0x000F) << 4));
        WriteReg(0x3500, b19_16);
        WriteReg(0x3501, b15_8);
        WriteReg(0x3502, b7_0);
    }

    public UInt16 GetVTS()
    {
        byte vts_h = ReadReg(0x380e);
        byte vts_l = ReadReg(0x380f);
        return (UInt16)((vts_h << 8) | vts_l);
    }

    public void SetVTS(UInt16 vts)
    {
        byte vts_h = (byte)(vts >> 8);
        byte vts_l = (byte)(vts & 0xFF);
        WriteReg(0x380e, vts_h);
        WriteReg(0x380f, vts_l);
    }

    public UInt16 GetHTS()
    {
        byte hts_h = ReadReg(0x380c);
        byte hts_l = ReadReg(0x380d);
        return (UInt16)((hts_h << 8) | hts_l);
    }

    public void SetHTS(UInt16 hts)
    {
        byte hts_h = (byte)(hts >> 8);
        byte hts_l = (byte)(hts & 0xFF);
        WriteReg(0x380c, hts_h);
        WriteReg(0x380d, hts_l);
    }

    public UInt16 GetExposure()
    {
        // Exposure: 20 bit, low 4 bits are fraction bits (ignored)
        byte b7_0, b15_8, b19_16;
        b19_16 = ReadReg(0x3500);
        b15_8 = ReadReg(0x3501);
        b7_0 = ReadReg(0x3502);
        UInt16 exposure = (UInt16)((b19_16 << 12) | (b15_8 << 4) | (b7_0 >> 4));
        return exposure;
    }

    // MWB (Manual White Balance)
    // Digital Gain for R,G,B channels. 0x400 is 1x gain.

    public bool GetWhiteBalanceEnable()
    {
        return (ReadReg(0x5000) & (1 << 4)) > 0;
    }

    public void SetWhiteBalanceEnable(bool val)
    {
        byte reg = ReadReg(0x5000);
        if (val)
        {
            WriteReg(0x5000, (byte)(reg | (1 << 4)));
        }
        else
        {
            WriteReg(0x5000, (byte)(reg & ~(1 << 4)));
        }
    }

    public void SetMWBGainRed(UInt16 gain)
    {
        WriteReg(0x5018, (byte)((gain & 0x3FC0) >> 6));
        WriteReg(0x5019, (byte)(gain & 0x3F));
    }
    public UInt16 GetMWBGainRed()
    {
        byte b13_6, b5_0;
        b13_6 = ReadReg(0x5018);
        b5_0 = ReadReg(0x5019);
        UInt16 gain = (UInt16)((b13_6 << 6) | (b5_0 & 0x3F));
        return gain;
    }

    public void SetMWBGainGreen(UInt16 gain)
    {
        WriteReg(0x501A, (byte)((gain & 0x3FC0) >> 6));
        WriteReg(0x501B, (byte)(gain & 0x3F));
    }
    public UInt16 GetMWBGainGreen()
    {
        byte b13_6, b5_0;
        b13_6 = ReadReg(0x501A);
        b5_0 = ReadReg(0x501B);
        UInt16 gain = (UInt16)((b13_6 << 6) | (b5_0 & 0x3F));
        return gain;
    }

    public void SetMWBGainBlue(UInt16 gain)
    {
        WriteReg(0x501C, (byte)((gain & 0x3FC0) >> 6));
        WriteReg(0x501D, (byte)(gain & 0x3F));
    }

    public UInt16 GetMWBGainBlue()
    {
        byte b13_6, b5_0;
        b13_6 = ReadReg(0x501C);
        b5_0 = ReadReg(0x501D);
        UInt16 gain = (UInt16)((b13_6 << 6) | (b5_0 & 0x3F));
        return gain;
    }

    /// <summary>
    /// Get Image into Raw Bayern pattern (BGGR)
    /// </summary>    
    public async Task<UInt16[]> GetImage(int width, int height, IProgress<double> progress)
    {
        _io.WriteByte(CMD_RD_IMG);
        UInt16[] img = new UInt16[width * height];

        var sw = Stopwatch.StartNew();
        // Read row by row
        for (int y = 0; y < height; y++)
        {
            byte[] row;

            if (_com == COM.COM_FT232H)
            {
                while (_fastIO.GetBytesAvailable() < width * 2)
                {
                    await Task.Delay(1);
                }
                row = _fastIO.Read(width * 2);
            }
            else
            {
                while (_io.GetBytesAvailable() < width * 2)
                {
                    await Task.Delay(1);
                }
                row = _io.Read(width * 2);
            }
            // Combine columns
            for (int x = 0; x < width; x++)
            {
                UInt16 pixel = (UInt16)((UInt16)row[x * 2] | (UInt16)(row[(x * 2) + 1] << 8));
                UInt16 pixel16 = (UInt16)Math.Round(pixel * (65535.0 / 1023.0));    // Convert 10 bit (0x03FF) to 16 bit (0xFFFF)
                img[(y * width) + x] = pixel16;
            }
            progress.Report((y + 1) * 100.0 / height);
        }
        sw.Stop();
        Debug.WriteLine($"Frame received in {sw.ElapsedMilliseconds}ms");
        CheckResponseOk();
        progress.Report(100.0);
        return img;
    }

    void CheckResponseOk()
    {
        var response = _io.ReadByte();
        if (response != 0xAA)
        {
            Debug.WriteLine($"0xAA expected, got {response.ToString("X")}");
            MessageBox.Show($"0xAA expected, got {response.ToString("X")}");
        }
    }

    public void SetFocus(UInt16 value)
    {
        _io.WriteByte(CMD_WR_FOCUS);
        _io.WriteUInt16(value);
        CheckResponseOk();
    }

    public UInt16 GetFocus()
    {
        _io.WriteByte(CMD_RD_FOCUS);
        UInt16 focus = (UInt16)(_io.ReadUInt16() / 16);
        CheckResponseOk();
        return focus;
    }

    /// <summary>
    /// If 0x3503[2]=0 then Gain is in real gain format.
    /// real_gain = gain[12:0] / 128 where gain[6:0] are fraction bits.
    /// Maximum is 16x analog gain.
    /// </summary>
    /// <param name="value"></param>
    public void SetAnalogGain(byte value)
    {
        WriteReg(0x3508, value);
    }

    public byte GetAnalogGain()
    {
        return ReadReg(0x3508);
    }
}