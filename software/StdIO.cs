// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
namespace CameraVision;

/// <summary>
/// Generic interface for IO device like FT232H, JTAG UART, RS232 UART, etc..
/// </summary>
public interface StdIO
{
    void Write(byte[] data);
    byte[] Read(int dataLenght);
    int GetBytesAvailable();
}

/// <summary>
/// Provide higher level functions for StdIO device
/// </summary>
public static class StdIOExtension
{
    public static void WriteUInt16(this StdIO stdio, UInt16 data)
    {
        stdio.Write(BitConverter.GetBytes(data));
    }
    public static void WriteUInt32(this StdIO stdio, UInt32 data)
    {
        stdio.Write(BitConverter.GetBytes(data));
    }
    public static void WriteFloat(this StdIO stdio, float data)
    {
        stdio.Write(BitConverter.GetBytes(data));
    }
    public static UInt16 ReadUInt16(this StdIO stdio)
    {
        return BitConverter.ToUInt16(stdio.Read(2), 0);
    }
    public static byte ReadByte(this StdIO stdio)
    {
        return stdio.Read(1)[0];
    }
    public static void WriteByte(this StdIO stdio, byte data)
    {
        stdio.Write(new byte[] { data });
    }
}