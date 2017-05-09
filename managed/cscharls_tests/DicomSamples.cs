// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Collections.Generic;
using Xunit;

using static CharLS.Util;

namespace CharLS
{
    public class DicomSamples
    {
        private static int findstring(byte[] container, byte[] bytesToFind, int bytesLength)
        {
            for (var i = 0; i < container.Length - bytesLength; ++i)
            {
                for (var j = 0; j < bytesLength; ++j)
                {
                    if (bytesToFind[j] != container[i + j])
                        goto next;
                }
                return i;

                next:
                ;
            }
            return -1;
        }

        private static void TestDicomSampleImage(string name)
        {
            var data = new byte[0];
            var success = ReadFile(name, ref data, 9);

            Assert.True(success);

            var pixeldataStart = new byte[] {0x00, 0x00, 0x01, 0x00, 0xFF, 0xD8, 0xFF, 0xF7};

            var offset = findstring(data, pixeldataStart, pixeldataStart.Length);

            var tmp = new List<byte>(data);
            tmp.RemoveRange(0, offset - 4);

            // remove the dicom fragment headers (in the concerned images they occur every 64k)
            for (var i = 0; i < data.Length; i += 64 * 1024)
            {
                tmp.RemoveRange(i, 8);
            }

            data = tmp.ToArray();

            JlsParameters parameters;
            var error = JpegLs.ReadHeader(data, out parameters);
            Assert.Equal(ApiResult.OK, error);

            //    0xFE, 0xFF, 0x00, 0xE0, 0x00, 0x00, 0x01, 0x00
            var dataUnc = new byte[parameters.stride * parameters.height];

            error = JpegLs.Decode(dataUnc, data);
            Assert.Equal(ApiResult.OK, error);
        }

        [Theory]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/XA1_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/CT2_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/MG1_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/MR1_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/MR2_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/MR3_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/MR4_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/NM1_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/RG1_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/RG2_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/RG3_JLSL")]
        [InlineData("test/compsamples_jpegls/IMAGES/JLSL/SC1_JLSL")]
        public void TestDicomWG4Images(string name)
        {
            TestDicomSampleImage(name);
        }
    }
}
