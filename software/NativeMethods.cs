// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)
// Documentation taken from https://github.com/thotypous/alterajtaguart

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Native methods exposed as C# methods
/// </summary>
public static class NativeMethods
{
    public enum JATL_ERROR
    {
        /// <summary>
        /// Unable to connect to local JTAG server
        /// </summary>
        UNABLE_TO_CONNECT_JTAG_SERVER = -1,
        /// <summary>
        /// More than one cable available, provide more specific cable name
        /// </summary>
        MULTIPLE_CABLE_AVAILABLE_PROVIDE_NAME = -2,
        /// <summary>
        /// Cable not available
        /// </summary>
        CABLE_NOT_AVAILABLE = -3,
        /// <summary>
        /// Selected cable is not plugged
        /// </summary>
        CABLE_NOT_PLUGGED = -4,
        /// <summary>
        /// JTAG not connected to board, or board powered down
        /// </summary>
        JTAG_NOT_AVAILABLE = -5,
        /// <summary>
        /// Another program (app_name) is already using the UART
        /// </summary>
        UART_ALREADY_USED = -6,
        /// <summary>
        /// More than one UART available, specify device/instance
        /// </summary>
        MULTIPLE_UART_AVAILABLE_PROVIDE_DEVICE_INSTANCE = -7,
        /// <summary>
        /// No UART matching the specified device/instance
        /// </summary>
        NO_UART_MATCHING = -8,
        /// <summary>
        /// Selected UART is not compatible with this version of the library
        /// </summary>
        UART_NOT_COMPATIBLE = -9
    }

    // API not implemented:
    // jtagatlantic_is_setup_done(struct JTAGATLANTIC *)        
    // jtagatlantic_open(char const *,int,int,char const *,class LOCK_HANDLER *)
    // jtagatlantic_scan_thread(struct JTAGATLANTIC *)

    [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string fileName);    

    /// <summary>
    /// Open a JTAG Atlantic UART
    /// </summary>
    /// <param name="cable">Identifies the USB Blaster connected to the device (e.g. ""DE-SoC [USB-1]""). If NULL, the library chooses at will</param>
    /// <param name="device">The number of the device inside the JTAG chain, starting from 1 for the first device. If -1, the library chooses at will</param>
    /// <param name="instance">The instance number of the JTAG Atlantic inside the device. If -1, the library chooses at will</param>        
    /// <returns>Handle to JTAGATLANTIC instance or 0 if error</returns>
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_open@@YAPEAUJTAGATLANTIC@@PEBDHH0@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr jtagatlantic_open(string cable, int device, int instance, string app_name);

    /// <summary>
    /// Close the UART
    /// </summary>        
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_close@@YAXPEAUJTAGATLANTIC@@@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void jtagatlantic_close(IntPtr handle);

    /// <summary>
    /// Read data from the UART
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="buffer">Pointer to data</param>
    /// <param name="bufferSize">Maximum amount of data to read</param>
    /// <returns>The number of chars received or -1 if connection was broken</returns>
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_read@@YAHPEAUJTAGATLANTIC@@PEADI@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern int jtagatlantic_read(IntPtr handle, byte[] buffer, int bufferSize);

    /// <summary>
    /// Wait for UART setup to be done.
    /// </summary>        
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_wait_open@@YAHPEAUJTAGATLANTIC@@@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void jtagatlantic_wait_open(IntPtr handle);

    /// <summary>
    /// Return number of bytes available for reading
    /// </summary>        
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_bytes_available@@YAHPEAUJTAGATLANTIC@@@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern int jtagatlantic_bytes_available(IntPtr handle);

    /// <summary>
    /// Wait for data to be flushed from the send buffer.
    /// </summary>        
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_flush@@YAHPEAUJTAGATLANTIC@@@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern int jtagatlantic_flush(IntPtr handle);

    /// <summary>
    /// Check if cable is good for JTAG UART communication.
    /// </summary>
    /// <param name="handle"></param>
    /// <returns>0 if the cable is adequate for JTAG UART communication.
    /// 2 otherwise (e.g. ByteBlaster, MasterBlaster or old USB-Blaster).</returns>
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_cable_warning@@YAHPEAUJTAGATLANTIC@@@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern int jtagatlantic_cable_warning(IntPtr handle);

    /// <summary>
    /// Get information about the UART to which we have actually connected.
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="cable">Pointer to the cable name. Memory is managed by the library, do not free the received pointer.</param>
    /// <param name="device">Device number.</param>
    /// <param name="instance">Instance number</param>
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_get_info@@YAXPEAUJTAGATLANTIC@@PEAPEBDPEAH2@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void jtagatlantic_get_info(IntPtr handle, ref IntPtr cable, out int device, out int instance);

    /// <summary>
    /// Get the last error ocurred.
    /// </summary>
    /// <param name="app_name">Pointer to a variable which will receive a pointer to the name of the program which is currently locking the UART (if error is -6).</param>
    /// <returns></returns>
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_get_error@@YA?AW4JATL_ERROR@@PEAPEBD@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern JATL_ERROR jtagatlantic_get_error(ref IntPtr app_name);

    /// <summary>
    /// Write data to the UART
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="buffer"></param>
    /// <param name="bufferSize"></param>
    /// <returns> the number of chars copied to send buffer or -1 if connection was broken</returns>
    [DllImport("jtag_atlantic.dll", EntryPoint = "?jtagatlantic_write@@YAHPEAUJTAGATLANTIC@@PEBDI@Z", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern int jtagatlantic_write(IntPtr handle, byte[] buffer, int bufferSize);

}