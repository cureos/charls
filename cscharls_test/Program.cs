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

                var bytes = File.ReadAllBytes("banny_normal.jls");
                var compressed = new ByteStreamInfo(bytes);

                JlsParameters parameters;
                string message = null;
                var result = JpegLS.ReadHeaderStream(compressed, out parameters, ref message);
                compressed.Position = 0;

                Console.WriteLine($"{result}: {message}");

                var stream = new MemoryStream();
                var decoded = new ByteStreamInfo(stream);
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
