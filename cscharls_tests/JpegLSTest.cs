// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
            Array.Copy(Enumerable.Repeat(3, 40000 - 900).ToArray(), 0, rgbyteCompressed, 900, 40000 - 900);

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

        [Fact]
        public void SuccessfullyDecodeLena()
        {
            byte[] toBytes = null;
            JlsParameters parameters = null;

            try
            {
                var fromBytes = File.ReadAllBytes("test/lena8b.jls");
                var compressed = new ByteStreamInfo(fromBytes);

                string message = null;
                var result = JpegLS.ReadHeaderStream(compressed, out parameters, ref message);
                compressed.Position = 0;

                Assert.Equal(ApiResult.OK, result);
                Assert.Equal(8, parameters.bitsPerSample);
                Assert.Equal(1, parameters.components);
                Assert.Equal(InterleaveMode.None, parameters.interleaveMode);

                toBytes = new byte[parameters.stride * parameters.height];
                var decoded = new ByteStreamInfo(toBytes);
                result = JpegLS.DecodeStream(decoded, compressed, parameters, ref message);

                Assert.Equal(ApiResult.OK, result);
            }
            catch (Exception e)
            {
                Assert.Equal(null, e);
            }
            finally
            {
                var bitmap = new Bitmap(parameters.width, parameters.height, PixelFormat.Format8bppIndexed);

                var palette = bitmap.Palette;
                for (var i = 0; i < palette.Entries.Length; ++i) palette.Entries[i] = Color.FromArgb(i, i, i);
                bitmap.Palette = palette;

                var data = bitmap.LockBits(
                    new Rectangle(0, 0, parameters.width, parameters.height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format8bppIndexed);
                Marshal.Copy(toBytes, 0, data.Scan0, toBytes.Length);
                bitmap.UnlockBits(data);

                bitmap.Save("lena8b.jpg");
            }
        }

        [Theory]
        [InlineData("banny_normal")]
        [InlineData("banny_HP1")]
        [InlineData("banny_HP2")]
        [InlineData("banny_HP3")]
        public void SuccessfullyDecodeRGB(string name)
        {
            byte[] toBytes = null;
            JlsParameters parameters = null;

            try
            {
                var fromBytes = File.ReadAllBytes($"test/{name}.jls");
                var compressed = new ByteStreamInfo(fromBytes);

                string message = null;
                var result = JpegLS.ReadHeaderStream(compressed, out parameters, ref message);
                compressed.Position = 0;

                Assert.Equal(ApiResult.OK, result);
                Assert.Equal(8, parameters.bitsPerSample);
                Assert.Equal(3, parameters.components);
                Assert.Equal(InterleaveMode.Line, parameters.interleaveMode);

                toBytes = new byte[parameters.stride * parameters.height];
                var decoded = new ByteStreamInfo(toBytes);
                parameters.interleaveMode = InterleaveMode.None;
                result = JpegLS.DecodeStream(decoded, compressed, parameters, ref message);

                Assert.Equal(ApiResult.OK, result);
            }
            catch (Exception e)
            {
                Assert.Equal(null, e);
            }
            finally
            {
                var bitmap = new Bitmap(parameters.width, parameters.height, PixelFormat.Format24bppRgb);
                var data = bitmap.LockBits(
                    new Rectangle(0, 0, parameters.width, parameters.height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format24bppRgb);
                Marshal.Copy(toBytes, 0, data.Scan0, toBytes.Length);
                bitmap.UnlockBits(data);

                bitmap.Save($"{name}.jpg");
            }
        }
    }
}
