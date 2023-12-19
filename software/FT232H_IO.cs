// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using FTD2XX_NET;
using System;
using System.Threading;

namespace CameraVision;

public class FT232H_IO : IDisposable, StdIO
{
    FTDI ft232h = new FTDI();
    private bool _isDisposing;

    public FT232H_IO()
    {
        const byte latency = 1;             // default is 16ms, set to 1ms for better performance            
        const uint InTransferSize = 1024 * 16;   // Default TransferSize is 4k, 0x10000=65k

        FTDI.FT_STATUS ftStatus;

        ftStatus = ft232h.OpenByDescription("UM232H");
        if (ftStatus != FTDI.FT_STATUS.FT_OK)
        {
            Console.WriteLine("Failed to open device (error {0})", ftStatus);
            return;
        }

        uint deviceID = 0;
        ftStatus = ft232h.GetDeviceID(ref deviceID);
        if (ftStatus != FTDI.FT_STATUS.FT_OK)
        {
            Console.WriteLine("Failed to get Device ID (error {0})", ftStatus);
            return;
        }

        Console.WriteLine("Open Device ID: {0}", deviceID.ToString("X"));

        ftStatus = ft232h.SetBitMode(0xFF, FTD2XX_NET.FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
        if (ftStatus != FTDI.FT_STATUS.FT_OK)
        {
            Console.WriteLine("Failed to SetBitMode in reset mode (error {0})", ftStatus);
            ft232h.Close();
            return;
        }

        Thread.Sleep(10);

        ftStatus = ft232h.SetBitMode(0xFF, FTD2XX_NET.FTDI.FT_BIT_MODES.FT_BIT_MODE_SYNC_FIFO);
        if (ftStatus != FTDI.FT_STATUS.FT_OK)
        {
            Console.WriteLine("Failed to SetBitMode in 245 Sync FIFO mode (error {0})", ftStatus);
            ft232h.Close();
            return;
        }

        ftStatus = ft232h.SetLatency(latency);
        if (ftStatus != FTDI.FT_STATUS.FT_OK)
        {
            Console.WriteLine("Failed to SetLatency (error {0})", ftStatus);
            ft232h.Close();
            return;
        }

        ftStatus = ft232h.InTransferSize(InTransferSize);
        if (ftStatus != FTDI.FT_STATUS.FT_OK)
        {
            Console.WriteLine("Failed to change InTransferSize (error {0})", ftStatus);
            ft232h.Close();
            return;
        }

        ftStatus = ft232h.SetFlowControl(FTD2XX_NET.FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0, 0);
        if (ftStatus != FTDI.FT_STATUS.FT_OK)
        {
            Console.WriteLine("Failed to SetFlowControl with FT_FLOW_RTS_CTS type (error {0})", ftStatus);
            ft232h.Close();
            return;
        }

        ftStatus = ft232h.Purge(FTD2XX_NET.FTDI.FT_PURGE.FT_PURGE_RX | FTD2XX_NET.FTDI.FT_PURGE.FT_PURGE_TX);
        if (ftStatus != FTDI.FT_STATUS.FT_OK)
        {
            Console.WriteLine("Failed to Purge (error {0})", ftStatus);
            ft232h.Close();
            return;
        }
    }

    public int GetBytesAvailable()
    {
        uint bytesAvailable = 0;
        ft232h.GetRxBytesAvailable(ref bytesAvailable);
        return (int)bytesAvailable;
    }

    public byte[] Read(int dataLenght)
    {
        var buffer = new byte[dataLenght];
        uint bytesLeft = (uint)dataLenght;
        while (bytesLeft > 0)
        {
            uint bytesRed = 0;
            ft232h.Read(buffer, (uint)dataLenght, ref bytesRed);
            bytesLeft -= bytesRed;
        }
        return buffer;
    }

    public void Write(byte[] data)
    {
        uint numBytesWritten = 0;
        ft232h.Write(data, data.Length, ref numBytesWritten);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposing)
        {
            if (disposing)
            {
                ft232h?.Close();
            }
            _isDisposing = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
