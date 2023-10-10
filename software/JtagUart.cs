// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// UART over JTAG for Altera devices
/// </summary>
/// <remarks>Require jtag_atlantic.dll and jtag_client.dll provided with Quartus II</remarks>
public class JtagUart : IDisposable, StdIO
{
    /// <summary>
    /// Structure to store JTAG connection information
    /// </summary>
    public struct JTagConnectionInfo
    {
        public string Cable { get; set; }
        public int Device { get; set; }
        public int Instance { get; set; }
    }

    IntPtr _handle = IntPtr.Zero;

    /// <summary>
    /// Create an instance of JtagUart and open a connection
    /// </summary>
    /// <param name="app_name">App name, usefull to know which app has opened JTAG UART if connection is already opened</param>
    /// <param name="cable">Programmer, ex. USB Blaster</param>
    /// <param name="device">FPGA connected to JTAG chain</param>
    /// <param name="instance">Instance of JTAG_UART</param>
    public JtagUart(string app_name = null, string cable = null, int device = -1, int instance = -1)
    {
        // Add Quartus bin folder to PATH of current process if available
        string quartusRootDir = Environment.GetEnvironmentVariable("QUARTUS_ROOTDIR");
        if (!string.IsNullOrWhiteSpace(quartusRootDir))
        {            
            string quartusBinDir = Path.Combine(quartusRootDir, Environment.Is64BitOperatingSystem ? "bin64" : "bin");
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + quartusBinDir);
        }

        NativeMethods.LoadLibrary("jtag_atlantic.dll");
        Open(app_name, cable, device, instance);
    }

    private NativeMethods.JATL_ERROR GetError()
    {
        IntPtr pointer = IntPtr.Zero;
        return NativeMethods.jtagatlantic_get_error(ref pointer);
    }

    private void CheckConnection()
    {
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Connection is not opened.");
    }

    public void Open(string app_name = null, string cable = null, int device = -1, int instance = -1)
    {
        _handle = NativeMethods.jtagatlantic_open(cable, device, instance, app_name);
        if (_handle == IntPtr.Zero)
        {
            string error = Enum.GetName(typeof(NativeMethods.JATL_ERROR), GetError());
            throw new Exception(string.Format("Cannot open jtag cable: {0} device: {1} instance: {2} because {3}", cable, device, instance, error));
        }
        if (NativeMethods.jtagatlantic_cable_warning(_handle) != 0)
        {
            // BeMicro CV A9 use old USB-Blaster
            //throw new Exception(string.Format("Cable used (e.g. ByteBlaster, MasterBlaster or old USB-Blaster) is not good for JTAG UART communication."));
        }
        NativeMethods.jtagatlantic_wait_open(_handle);
    }

    public JTagConnectionInfo GetConnectionInfo()
    {
        CheckConnection();
        IntPtr cable = IntPtr.Zero;
        int device = 0;
        int instance = 0;
        NativeMethods.jtagatlantic_get_info(_handle, ref cable, out device, out instance);
        return new JTagConnectionInfo() { Cable = Marshal.PtrToStringAnsi(cable), Device = device, Instance = instance };
    }

    public int GetBytesAvailable()
    {
        CheckConnection();
        return NativeMethods.jtagatlantic_bytes_available(_handle);
    }

    public byte[] Read(int dataLenght)
    {
        CheckConnection();
        byte[] buffer = new byte[dataLenght];

        int remainingBytes = dataLenght;
        while (remainingBytes > 0)
        {
            int readBytes = NativeMethods.jtagatlantic_read(_handle, buffer, remainingBytes);
            if (readBytes == -1)
            {
                throw new Exception("Connection error during read");
            }
            remainingBytes -= readBytes;
        }
        return buffer;
    }    

    public void Write(byte[] data)
    {
        CheckConnection();
        int remainingBytes = data.Length;
        while (remainingBytes > 0)
        {
            int writtenBytes = NativeMethods.jtagatlantic_write(_handle, data, remainingBytes);
            if (writtenBytes == -1)
            {
                throw new Exception("Connection error during read");
            }
            remainingBytes -= writtenBytes;
        }
    }    

    public void Flush()
    {
        CheckConnection();
        NativeMethods.jtagatlantic_flush(_handle);
    }

    public int GetAvailableBytes()
    {
        CheckConnection();
        return NativeMethods.jtagatlantic_bytes_available(_handle);
    }

    public string ReadLine()
    {
        StringBuilder sb = new StringBuilder(80);
        byte character = this.ReadByte();
        while (character != '\n')
        {
            sb.Append(Encoding.ASCII.GetString(new byte[] { character }));
            character = this.ReadByte();
        }
        sb.Append('\n');
        return sb.ToString();
    }

    public void Close()
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.jtagatlantic_close(_handle);
            _handle = IntPtr.Zero;
        }
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects).
            }

            // free unmanaged resources (unmanaged objects) and override a finalizer below.
            // set large fields to null.
            Close();

            disposedValue = true;
        }
    }

    ~JtagUart()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

}