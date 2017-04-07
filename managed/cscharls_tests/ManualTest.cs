// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace CharLS
{
    public class ManualTest
    {
        [Fact]
        public void SuccessfullyDecodeLena()
        {
            byte[] toBytes = null;
            JlsParameters parameters = null;

            try
            {
                var compressed = File.ReadAllBytes("test/lena8b.jls");

                string message;
                var result = JpegLs.ReadHeader(compressed, out parameters, out message);

                Assert.Equal(ApiResult.OK, result);
                Assert.Equal(8, parameters.bitsPerSample);
                Assert.Equal(1, parameters.components);
                Assert.Equal(InterleaveMode.None, parameters.interleaveMode);

                toBytes = new byte[parameters.stride * parameters.height];
                result = JpegLs.Decode(compressed, toBytes, parameters, out message);

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
                var compressed = File.ReadAllBytes($"test/{name}.jls");

                string message;
                var result = JpegLs.ReadHeader(compressed, out parameters, out message);

                Assert.Equal(ApiResult.OK, result);
                Assert.Equal(8, parameters.bitsPerSample);
                Assert.Equal(3, parameters.components);
                Assert.Equal(InterleaveMode.Line, parameters.interleaveMode);

                toBytes = new byte[parameters.stride * parameters.height];
                parameters.interleaveMode = InterleaveMode.None;
                result = JpegLs.Decode(compressed, toBytes, parameters, out message);

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

        [Fact]
        public void SuccessfullyEncodeLena()
        {
            var inBytes = File.ReadAllBytes("test/lena8b.raw");
            var outBytes = new byte[inBytes.Length];

            ulong bytesWritten;
            var parameters = new JlsParameters { bitsPerSample = 8, components = 1, height = 512, width = 512 };
            string message;

            var result = JpegLs.Encode(inBytes, outBytes, parameters, out bytesWritten, out message);
            Assert.Equal(ApiResult.OK, result);

            Array.Resize(ref outBytes, (int)bytesWritten);
            File.WriteAllBytes("lena8b.jls", outBytes);
        }
    }
}
