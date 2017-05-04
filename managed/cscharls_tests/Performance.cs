// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Diagnostics;
using Xunit;

using static CharLS.Util;

namespace CharLS
{
    public class Performance
    {
        private static void TestFile16BitAs12(string strName, int ioffs, Size size2, int ccomp, bool littleEndianFile)
        {
            var rgbyteUncompressed = new byte[0];
            if (!ReadFile(strName, ref rgbyteUncompressed, ioffs))
                return;

            FixEndian(rgbyteUncompressed, littleEndianFile);

            var pushortLen = rgbyteUncompressed.Length / 2;
            var pushort = new ushort[pushortLen];

            ByteArray.Copy(rgbyteUncompressed, 0, pushort, 0, pushortLen);
            for (var i = 0; i < pushortLen; ++i)
            {
                pushort[i] = (ushort) (pushort[i] >> 4);
            }
            ByteArray.Copy(pushort, 0, pushortLen, rgbyteUncompressed, 0);

            TestRoundTrip(strName, rgbyteUncompressed, size2, 12, ccomp);
        }


        private static void TestPerformance(int loopCount)
        {
            ////TestFile("test/bad.raw", 0, new Size(512, 512),  8, 1);

            // RGBA image (This is a common PNG sample)
            TestFile("test/alphatest.raw", 0, new Size(380, 287), 8, 4, false, loopCount);

            var size1024 = new Size(1024, 1024);
            var size512 = new Size(512, 512);

            // 16 bit mono
            TestFile("test/MR2_UNC", 1728, size1024, 16, 1, true, loopCount);

            // 8 bit mono
            TestFile("test/0015.raw", 0, size1024, 8, 1, false, loopCount);
            TestFile("test/lena8b.raw", 0, size512, 8, 1, false, loopCount);

            // 8 bit color
            TestFile("test/desktop.ppm", 40, new Size(1280, 1024), 8, 3, false, loopCount);

            // 12 bit RGB
            TestFile("test/SIEMENS-MR-RGB-16Bits.dcm", -1, new Size(192, 256), 12, 3, true, loopCount);
            TestFile16BitAs12("test/DSC_5455.raw", 142949, new Size(300, 200), 3, true);

            // 16 bit RGB
            TestFile("test/DSC_5455.raw", 142949, new Size(300, 200), 16, 3, true, loopCount);
        }


        private static void TestLargeImagePerformance(int loopCount)
        {
            TestFile("test/rgb8bit/artificial.ppm", 17, new Size(3072, 2048), 8, 3, false, loopCount);
            TestFile("test/rgb8bit/bridge.ppm", 17, new Size(2749, 4049), 8, 3, false, loopCount);
            TestFile("test/rgb8bit/flower_foveon.ppm", 17, new Size(2268, 1512), 8, 3, false, loopCount);
            ////TestFile("test/rgb8bit/big_building.ppm", 17, new Size(7216,5412),  8, 3);
            ////TestFile("test/rgb16bit/bridge.ppm", 19, new Size(2749,4049),  16, 3, true);
        }


        [Theory]
        [InlineData(1)]
        public void PerformanceTests(int loopCount)
        {
            Console.WriteLine($"Test Perf (with loop count {loopCount})");
            TestPerformance(loopCount);

#if TESTLARGE
            Console.WriteLine("Test Large Images Performance");
            TestLargeImagePerformance(loopCount);
#endif
        }

        [Theory]
        [InlineData(1)]
        public void DecodePerformanceTests(int loopCount)
        {
            Console.WriteLine($"Test decode Perf (with loop count {loopCount})");

            var jpeglsCompressed = new byte[0];
            if (!ReadFile("test/decodetest.jls", ref jpeglsCompressed, 0, 0))
            {
                Assert.True(false, "Failed to load the file decodetest.jls");
            }

            JlsParameters parameters;
            string message;
            var result = JpegLs.ReadHeader(jpeglsCompressed, out parameters, out message);
            Assert.Equal(ApiResult.OK, result);

            var uncompressed = new byte[parameters.height * parameters.width * 2];

            var start = new Stopwatch();
            start.Start();
            for (var i = 0; i < loopCount; ++i)
            {
                result = JpegLs.Decode(uncompressed, jpeglsCompressed, parameters, out message);
                Assert.Equal(ApiResult.OK, result);
            }

            start.Stop();
            var diff = start.ElapsedMilliseconds;
            Console.WriteLine($"Total decoding time is: {diff} ms");
            Console.WriteLine($"Decoding time per image: {diff / loopCount} ms");
        }
    }
}
