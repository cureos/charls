// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Diagnostics;
using System.IO;

using Xunit;

namespace CharLS
{
    internal struct Size
    {
        internal Size(int width, int height)
        {
            cx = width;
            cy = height;
        }

        internal readonly int cx;
        internal readonly int cy;
    }

    internal static class Util
    {
        private static void FixEndian(byte[] rgbyte, bool littleEndianData)
        {
            if (littleEndianData == BitConverter.IsLittleEndian)
                return;

            for (var i = 0; i < rgbyte.Length - 1; i += 2)
            {
                var tmp = rgbyte[i];
                rgbyte[i] = rgbyte[i + 1];
                rgbyte[i + 1] = tmp;
            }
        }

        internal static bool ReadFile(string strName, byte[] pvec, int offset, int bytes)
        {
            if (!File.Exists(strName))
            {
                Console.WriteLine($"Could not open {strName}");
                return false;
            }

            using (var pfile = File.OpenRead(strName))
            {
                var cbyteFile = (int) pfile.Length;
                if (offset < 0)
                {
                    Assert.True(bytes != 0);
                    offset = cbyteFile - bytes;
                }
                if (bytes == 0)
                {
                    bytes = cbyteFile - offset;
                }

                pfile.Seek(offset, SeekOrigin.Begin);
                Array.Resize(ref pvec, bytes);
                var bytesRead = pfile.Read(pvec, 0, bytes);
                return bytesRead == bytes;
            }
        }

        internal static void WriteFile(string strName, byte[] vec)
        {
            if (!File.Exists(strName))
            {
                Console.WriteLine($"Could not open {strName}");
                return;
            }

            File.WriteAllBytes(strName, vec);
        }

        internal static void TestRoundTrip(string strName, byte[] rgbyteRaw, Size size, int cbit, int ccomp,
            int loopCount)
        {
            var rgbyteCompressed = new byte[size.cx * size.cy * ccomp * cbit / 4];

            var rgbyteOut = new byte[size.cx * size.cy * ((cbit + 7) / 8) * ccomp];

            var parameters = new JlsParameters
            {
                components = ccomp,
                bitsPerSample = cbit,
                height = size.cy,
                width = size.cx
            };

            if (ccomp == 4)
            {
                parameters.interleaveMode = InterleaveMode.Line;
            }
            else if (ccomp == 3)
            {
                parameters.interleaveMode = InterleaveMode.Line;
                parameters.colorTransformation = ColorTransformation.HP1;
            }

            ulong compressedLength = 0;
            string message;
            var dwtimeEncodeStart = new Stopwatch();
            dwtimeEncodeStart.Start();
            for (int i = 0; i < loopCount; ++i)
            {
                var err = JpegLs.Encode(rgbyteCompressed, rgbyteRaw, parameters, out compressedLength, out message);
                Assert.True(err == ApiResult.OK);
            }
            double dwtimeEncodeComplete = dwtimeEncodeStart.ElapsedMilliseconds;

            var dwtimeDecodeStart = new Stopwatch();
            dwtimeDecodeStart.Start();
            for (int i = 0; i < loopCount; ++i)
            {
                var err = JpegLs.Decode(rgbyteOut, rgbyteCompressed, null, out message);
                Assert.True(err == ApiResult.OK);
            }
            double dwtimeDecodeComplete = dwtimeDecodeStart.ElapsedMilliseconds;

            double bitspersample = compressedLength * 8 * 1.0 / (ccomp * size.cy * size.cx);
            Console.WriteLine($"RoundTrip test for: {strName}");
            double encodeTime = dwtimeEncodeComplete / loopCount;
            double decodeTime = dwtimeDecodeComplete / loopCount;
            double symbolRate = (ccomp * size.cy * size.cx) / (1000.0 * decodeTime);
            Console.WriteLine(
                $"Size:{size.cx:####}{size.cy:####}, Encode time:{encodeTime:####.00} ms, Decode time:{decodeTime:####.00} ms, Bits per sample:{bitspersample:##.00}, Decode rate:{symbolRate:###.0} M/s");
        }

        internal static void TestFile(string strName, int ioffs, Size size2, int cbit, int ccomp, bool littleEndianFile,
            int loopCount)
        {
            int byteCount = size2.cx * size2.cy * ccomp * ((cbit + 7) / 8);
            byte[] rgbyteUncompressed = new byte[0];

            if (!ReadFile(strName, rgbyteUncompressed, ioffs, byteCount))
                return;

            if (cbit > 8)
            {
                FixEndian(rgbyteUncompressed, littleEndianFile);
            }

            TestRoundTrip(strName, rgbyteUncompressed, size2, cbit, ccomp, loopCount);
        }
    }
}
