// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace CharLS
{
    public class JpegLSTest
    {
        [Fact]
        public void TestDecodeBitStreamWithNoMarkerStart()
        {
            byte[] encodedData = { 0x33, 0x33 };
            var output = new byte[1000];
            string message = null;

            var error = JpegLS.DecodeStream(output, encodedData, null, ref message);
            Assert.Equal(error, ApiResult.MissingJpegMarkerStart);
        }

        [Fact]
        public void TestDecodeBitStreamWithUnsupportedEncoding()
        {
            byte[] encodedData =
                {
                    0xFF, 0xD8, // Start Of Image (JPEG_SOI)
                    0xFF, 0xC3, // Start Of Frame (lossless, huffman) (JPEG_SOF_3)
                    0x00, 0x00 // Lenght of data of the marker
                };
            var output = new byte[1000];
            string message = null;

            var error = JpegLS.DecodeStream(output, encodedData, null, ref message);
            Assert.Equal(error, ApiResult.UnsupportedEncoding);
        }

        [Fact]
        public void TestDecodeBitStreamWithUnknownJpegMarker()
        {
            byte[] encodedData =
                {
                    0xFF, 0xD8, // Start Of Image (JPEG_SOI)
                    0xFF, 0x01, // Undefined marker
                    0x00, 0x00 // Lenght of data of the marker
                };
            var output = new byte[1000];
            string message = null;

            var error = JpegLS.DecodeStream(output, encodedData, null, ref message);
            Assert.Equal(error, ApiResult.UnknownJpegMarker);
        }

        [Fact]
        public void TestDamagedBitStream2()
        {
            var rgbyteCompressed = File.ReadAllBytes("test/lena8b.jls");
            var rgbyteOut = new byte[512 * 512];
            string message = null;

            Array.Resize(ref rgbyteCompressed, 900);
            Array.Resize(ref rgbyteCompressed, 40000);
            Array.Copy(Enumerable.Repeat((byte)3, 40000 - 900).ToArray(), 0, rgbyteCompressed, 900, 40000 - 900);

            var error = JpegLS.DecodeStream(rgbyteOut, rgbyteCompressed, null, ref message);
            Assert.Equal(error, ApiResult.InvalidCompressedData);
        }


        [Fact]
        public void TestDamagedBitStream3()
        {
            var rgbyteCompressed = File.ReadAllBytes("test/lena8b.jls");
            var rgbyteOut = new byte[512 * 512];
            string message = null;

            rgbyteCompressed[300] = 0xFF;
            rgbyteCompressed[301] = 0xFF;

            var error = JpegLS.DecodeStream(rgbyteOut, rgbyteCompressed, null, ref message);
            Assert.Equal(error, ApiResult.InvalidCompressedData);
        }
    }
}
