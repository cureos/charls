// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using Xunit;

using static CharLS.Util;

namespace CharLS
{
    public class Compliance
    {
        private static void Triplet2Planar(byte[] rgbyte, Size size)
        {
            var rgbytePlanar = new byte[rgbyte.Length];

            var cbytePlane = size.cx * size.cy;
            for (var index = 0; index < cbytePlane; index++)
            {
                rgbytePlanar[index] = rgbyte[index * 3 + 0];
                rgbytePlanar[index + 1 * cbytePlane] = rgbyte[index * 3 + 1];
                rgbytePlanar[index + 2 * cbytePlane] = rgbyte[index * 3 + 2];
            }
            Array.Copy(rgbytePlanar, rgbyte, rgbyte.Length);
        }

        private static void VerifyEncodedBytes(byte[] uncompressedData, byte[] compressedData)
        {
            JlsParameters info;
            string message;
            Assert.Equal(ApiResult.OK, JpegLs.ReadHeader(compressedData, out info, out message));

            var ourEncodedBytes = new byte[compressedData.Length + 16];
            ulong bytesWriten;
            Assert.Equal(ApiResult.OK,
                JpegLs.Encode(ourEncodedBytes, uncompressedData, info, out bytesWriten, out message));

            Array.Resize(ref ourEncodedBytes, compressedData.Length);
            Assert.Equal(compressedData, ourEncodedBytes);
        }

        private static void TestCompliance(byte[] compressedBytes, byte[] rgbyteRaw, bool bcheckEncode)
        {
            JlsParameters info;
            string message;
            var err = JpegLs.ReadHeader(compressedBytes, out info, out message);
            Assert.Equal(ApiResult.OK, err);

            if (bcheckEncode)
            {
                VerifyEncodedBytes(rgbyteRaw, compressedBytes);
            }

            var rgbyteOut = new byte[info.height * info.width * ((info.bitsPerSample + 7) / 8) * info.components];

            err = JpegLs.Decode(rgbyteOut, compressedBytes, null, out message);
            Assert.Equal(ApiResult.OK, err);

            if (info.allowedLossyError == 0)
            {
                Array.Resize(ref rgbyteOut, rgbyteRaw.Length);
                Assert.Equal(rgbyteRaw, rgbyteOut);
            }
        }


        private static void DecompressFile(string strNameEncoded, string strNameRaw, int ioffs,
            bool bcheckEncode)
        {
            Console.WriteLine($"Conformance test: {strNameEncoded}");
            var rgbyteFile = new byte[0];
            Assert.True(ReadFile(strNameEncoded, ref rgbyteFile));

            JlsParameters parameters;
            string message;
            Assert.Equal(ApiResult.OK, JpegLs.ReadHeader(rgbyteFile, out parameters, out message));

            var rgbyteRaw = new byte[0];
            Assert.True(ReadFile(strNameRaw, ref rgbyteRaw, ioffs));

            if (parameters.bitsPerSample > 8)
            {
                FixEndian(rgbyteRaw, false);
            }

            if (parameters.interleaveMode == InterleaveMode.None && parameters.components == 3)
            {
                Triplet2Planar(rgbyteRaw, new Size(parameters.width, parameters.height));
            }

            TestCompliance(rgbyteFile, rgbyteRaw, bcheckEncode);
        }

////private byte[] palettisedDataH10 = {
////    0xFF, 0xD8, //Start of image (SOI) marker 
////    0xFF, 0xF7, //Start of JPEG-LS frame (SOF 55) marker – marker segment follows 
////    0x00, 0x0B, //Length of marker segment = 11 bytes including the length field 
////    0x02, //P = Precision = 2 bits per sample 
////    0x00, 0x04, //Y = Number of lines = 4 
////    0x00, 0x03, //X = Number of columns = 3 
////    0x01, //Nf = Number of components in the frame = 1 
////    0x01, //C1  = Component ID = 1 (first and only component) 
////    0x11, //Sub-sampling: H1 = 1, V1 = 1 
////    0x00, //Tq1 = 0 (this field is always 0) 
////
////    0xFF, 0xF8, //LSE – JPEG-LS preset parameters marker 
////    0x00, 0x11, //Length of marker segment = 17 bytes including the length field 
////    0x02, //ID = 2, mapping table  
////    0x05, //TID = 5 Table identifier (arbitrary) 
////    0x03, //Wt = 3 Width of table entry 
////    0xFF, 0xFF, 0xFF, //Entry for index 0 
////    0xFF, 0x00, 0x00, //Entry for index 1 
////    0x00, 0xFF, 0x00, //Entry for index 2 
////    0x00, 0x00, 0xFF, //Entry for index 3 
////
////    0xFF, 0xDA, //Start of scan (SOS) marker 
////    0x00, 0x08, //Length of marker segment = 8 bytes including the length field 
////    0x01, //Ns = Number of components for this scan = 1 
////    0x01, //C1 = Component ID = 1  
////    0x05, //Tm 1  = Mapping table identifier = 5 
////    0x00, //NEAR = 0 (near-lossless max error) 
////    0x00, //ILV = 0 (interleave mode = non-interleaved) 
////    0x00, //Al = 0, Ah = 0 (no point transform) 
////    0xDB, 0x95, 0xF0, //3 bytes of compressed image data 
////    0xFF, 0xD9 //End of image (EOI) marker 
////};

        private static readonly byte[] _rgbyte =
        {
            0, 0, 90, 74,
            68, 50, 43, 205,
            64, 145, 145, 145,
            100, 145, 145, 145
        };

////private byte[] rgbyteComp =   
////{   0xFF, 0xD8, 0xFF, 0xF7, 0x00, 0x0B, 0x08, 0x00, 0x04, 0x00, 0x04, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 
////0xC0, 0x00, 0x00, 0x6C, 0x80, 0x20, 0x8E,
////0x01, 0xC0, 0x00, 0x00, 0x57, 0x40, 0x00, 0x00, 0x6E, 0xE6, 0x00, 0x00, 0x01, 0xBC, 0x18, 0x00,
////0x00, 0x05, 0xD8, 0x00, 0x00, 0x91, 0x60, 0xFF, 0xD9};

        public void TestSampleAnnexH3()
        {
            ////Size size = new Size(4,4);
            var vecRaw = new byte[16];
            Array.Copy(_rgbyte, vecRaw, 16);
            ////  TestJls(vecRaw, size, 8, 1, ILV_NONE, rgbyteComp, sizeof(rgbyteComp), false);
        }

        [Theory]
        [InlineData("test/jlsimage/banny_normal.jls", "test/jlsimage/banny.ppm", 38, false)]
        [InlineData("test/jlsimage/banny_HP1.jls", "test/jlsimage/banny.ppm", 38, false)]
        [InlineData("test/jlsimage/banny_HP2.jls", "test/jlsimage/banny.ppm", 38, false)]
        [InlineData("test/jlsimage/banny_HP3.jls", "test/jlsimage/banny.ppm", 38, false)]
        [InlineData("test/conformance/T8C0E0.JLS", "test/conformance/TEST8.PPM", 15, true)]
        [InlineData("test/conformance/T8C1E0.JLS", "test/conformance/TEST8.PPM", 15, true)]
        [InlineData("test/conformance/T8C2E0.JLS", "test/conformance/TEST8.PPM", 15, true)]
        [InlineData("test/conformance/T8C0E3.JLS", "test/conformance/TEST8.PPM", 15, true)]
        [InlineData("test/conformance/T8C1E3.JLS", "test/conformance/TEST8.PPM", 15, true)]
        [InlineData("test/conformance/T8C2E3.JLS", "test/conformance/TEST8.PPM", 15, true)]
        [InlineData("test/conformance/T8NDE0.JLS", "test/conformance/TEST8BS2.PGM", 15, true)]
        [InlineData("test/conformance/T8NDE3.JLS", "test/conformance/TEST8BS2.PGM", 15, true)]
        [InlineData("test/conformance/T16E0.JLS", "test/conformance/TEST16.PGM", 16, true)]
        [InlineData("test/conformance/T16E3.JLS", "test/conformance/TEST16.PGM", 16, true)]
        [InlineData("test/lena8b.jls", "test/lena8b.raw", 0, true)]
        public void TestConformance(string strNameEncoded, string strNameRaw, int ioffs, bool bcheckEncode)
        {
            DecompressFile(strNameEncoded, strNameRaw, ioffs, bcheckEncode);
        }
    }
}
