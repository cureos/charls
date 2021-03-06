﻿// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Diagnostics;
using Xunit;
using Xunit.Sdk;

using static CharLS.Util;

namespace CharLS
{
    public class Performance
    {
        [Theory]
        [InlineData("test/DSC_5455.raw", 142949, 300, 200, 3, true)]
        public void TestFile16BitAs12(string strName, int ioffs, int width, int height, int ccomp, bool littleEndianFile)
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

            TestRoundTrip(strName, rgbyteUncompressed, new Size(width, height), 12, ccomp);
        }

        [Theory]
        [InlineData("test/bad.raw", 0, 512, 512,  8, 1, false, 1, Skip = "File not available")]
        [InlineData("test/alphatest.raw", 0, 380, 287, 8, 4, false, 1)] // RGBA image (This is a common PNG sample)
        [InlineData("test/MR2_UNC", 1728, 1024, 1024, 16, 1, true, 1)] // 16 bit mono
        [InlineData("test/0015.raw", 0, 1024, 1024, 8, 1, false, 1)] // 8 bit mono
        [InlineData("test/lena8b.raw", 0, 512, 512, 8, 1, false, 1)] // 8 bit mono
        [InlineData("test/desktop.ppm", 40, 1280, 1024, 8, 3, false, 1)] // 8 bit color
        [InlineData("test/SIEMENS-MR-RGB-16Bits.dcm", -1, 192, 256, 12, 3, true, 1)] // 12 bit RGB
        [InlineData("test/DSC_5455.raw", 142949, 300, 200, 16, 3, true, 1)] // 16 bit RGB
        [InlineData("test/rgb8bit/artificial.ppm", 17, 3072, 2048, 8, 3, false, 1, Skip = "Long running")] // 16 bit RGB
        [InlineData("test/rgb8bit/bridge.ppm", 17, 2749, 4049, 8, 3, false, 1, Skip = "Long running")] // 16 bit RGB
        [InlineData("test/rgb8bit/flower_foveon.ppm", 17, 2268, 1512, 8, 3, false, 1, Skip = "Long running")] // 16 bit RGB
        [InlineData("test/rgb8bit/big_building.ppm", 17, 7216,5412,  8, 3, false, 1, Skip = "Long running")]
        [InlineData("test/rgb16bit/bridge.ppm", 19, 2749,4049,  16, 3, true, 1, Skip = "Long running")]
        public void PerformanceTests(string strName, int ioffs, int width, int height, int cbit, int ccomp,
            bool littleEndianFile, int loopCount)
        { 
            TestFile(strName, ioffs, new Size(width, height), cbit, ccomp, littleEndianFile, loopCount);
        }

        [Theory(Skip = "File not available")]
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
            var result = JpegLs.ReadHeader(jpeglsCompressed, out parameters);
            Assert.Equal(ApiResult.OK, result);

            var uncompressed = new byte[parameters.height * parameters.width * 2];

            var start = new Stopwatch();
            start.Start();
            for (var i = 0; i < loopCount; ++i)
            {
                result = JpegLs.Decode(uncompressed, jpeglsCompressed);
                Assert.Equal(ApiResult.OK, result);
            }

            start.Stop();
            var diff = start.ElapsedMilliseconds;
            Console.WriteLine($"Total decoding time is: {diff} ms");
            Console.WriteLine($"Decoding time per image: {diff / loopCount} ms");
        }
    }
}
