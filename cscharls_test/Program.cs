using System;
using System.IO;

using CharLS;

namespace cscharls_test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                var fromBytes = File.ReadAllBytes("banny_normal.jls");
                var compressed = new ByteStreamInfo(fromBytes);

                JlsParameters parameters;
                string message = null;
                var result = JpegLS.ReadHeaderStream(compressed, out parameters, ref message);
                compressed.Position = 0;

                Console.WriteLine($"{result}: {message}");

                var toBytes = new byte[parameters.stride * parameters.height];
                var decoded = new ByteStreamInfo(toBytes);
                result = JpegLS.DecodeStream(decoded, compressed, parameters, ref message);

                Console.WriteLine($"{result}: {message}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
        }
    }
}
