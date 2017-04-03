using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

using CharLS;

namespace cscharls_test
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] toBytes = null;
            JlsParameters parameters = null;

            try
            {

                var fromBytes = File.ReadAllBytes("banny_normal.jls");
                var compressed = new ByteStreamInfo(fromBytes);

                string message = null;
                var result = JpegLS.ReadHeaderStream(compressed, out parameters, ref message);
                compressed.Position = 0;

                Console.WriteLine($"{result}: {message}");

                toBytes = new byte[parameters.stride * parameters.height];
                var decoded = new ByteStreamInfo(toBytes);
                result = JpegLS.DecodeStream(decoded, compressed, parameters, ref message);

                Console.WriteLine($"{result}: {message}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine($"Interleave mode: {parameters.interleaveMode}");

                var bitmap = new Bitmap(parameters.width, parameters.height);
                var data = bitmap.LockBits(
                    new Rectangle(0, 0, parameters.width, parameters.height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format24bppRgb);
                Marshal.Copy(toBytes, 0, data.Scan0, toBytes.Length);
                bitmap.UnlockBits(data);
                bitmap.Save("result.jpg");
            }

            Console.ReadLine();
        }
    }
}
