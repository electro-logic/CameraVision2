// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System.Diagnostics;

namespace CameraVision
{
    public static class ExifTool
    {
        public static void DNGtoTIFF(string filename, string filenameTiff)
        {
            var exifProcess = Process.Start(new ProcessStartInfo("exiftool.exe", $"-DNGVersion= -PhotometricInterpretation=\"BlackIsZero\" -o \"{filenameTiff}\" \"{filename}\"")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            exifProcess.WaitForExit();
            Debug.Write($"ExifTool Output: {exifProcess.StandardOutput.ReadToEnd()}");
            Debug.Write($"ExifTool Error: {exifProcess.StandardError.ReadToEnd()}");
        }
    }
}
