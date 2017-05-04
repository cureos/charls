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
        public ManualTest()
        {
            if (!Directory.Exists("out")) Directory.CreateDirectory("out");
        }

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
                result = JpegLs.Decode(toBytes, compressed, parameters, out message);

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

                bitmap.Save("out/lena8b.jpg");
            }
        }

        [Fact]
        public void SuccessfullyDecodeLenaStream()
        {
            var toStream = new MemoryStream();
            JlsParameters parameters = new JlsParameters();

            try
            {
                var compressed = File.OpenRead("test/lena8b.jls");

                string message;
                var result = JpegLs.ReadHeader(compressed, out parameters, out message);

                Assert.Equal(ApiResult.OK, result);
                Assert.Equal(8, parameters.bitsPerSample);
                Assert.Equal(1, parameters.components);
                Assert.Equal(InterleaveMode.None, parameters.interleaveMode);

                result = JpegLs.Decode(toStream, compressed, parameters, out message);

                Assert.Equal(ApiResult.OK, result);
            }
            catch (Exception e)
            {
                Assert.Equal(null, e);
            }
            finally
            {
                var toBytes = toStream.ToArray();
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

                bitmap.Save("out/lena8b_s.jpg");
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
                var compressed = File.ReadAllBytes($"test/jlsimage/{name}.jls");

                string message;
                var result = JpegLs.ReadHeader(compressed, out parameters, out message);

                Assert.Equal(ApiResult.OK, result);
                Assert.Equal(8, parameters.bitsPerSample);
                Assert.Equal(3, parameters.components);
                Assert.Equal(InterleaveMode.Line, parameters.interleaveMode);

                toBytes = new byte[parameters.stride * parameters.height];
                parameters.outputBgr = true;    // Bitmap Format24bppRgb is sorted BGR

                result = JpegLs.Decode(toBytes, compressed, parameters, out message);

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
                    bitmap.PixelFormat);

                if (parameters.stride == data.Stride)
                {
                    Marshal.Copy(toBytes, 0, data.Scan0, toBytes.Length);
                }
                else
                {
                    int srcOffs = 0, stride = Math.Min(parameters.stride, data.Stride);
                    var destOffs = data.Scan0.ToInt64();

                    for (var j = 0; j < parameters.height; ++j, srcOffs += parameters.stride, destOffs += data.Stride)
                        Marshal.Copy(toBytes, srcOffs, new IntPtr(destOffs), stride);
                }

                bitmap.UnlockBits(data);
                bitmap.Save($"out/{name}.jpg");
            }
        }

        [Theory]
        [InlineData("0015", 8, 1, 1024, 1024, InterleaveMode.None)]
        [InlineData("alphatest", 8, 4, 380, 287, InterleaveMode.Line)]
        [InlineData("DSC_5455", 16, 3, 300, 200, InterleaveMode.Line)]
        [InlineData("lena8b", 8, 1, 512, 512, InterleaveMode.None)]
        public void SuccessfullyEncodeRaw(string name, int bitsPerSample, int components, int height, int width, InterleaveMode mode)
        {
            var inBytes = File.ReadAllBytes($"test/{name}.raw");
            var outBytes = new byte[inBytes.Length];

            ulong bytesWritten;
            string message;
            var parameters = new JlsParameters
            {
                bitsPerSample = bitsPerSample,
                components = components,
                height = height,
                width = width,
                interleaveMode = mode
            };

            var result = JpegLs.Encode(outBytes, inBytes, parameters, out bytesWritten, out message);
            Assert.Equal(ApiResult.OK, result);

            Array.Resize(ref outBytes, (int)bytesWritten);
            File.WriteAllBytes($"out/{name}.jls", outBytes);
        }
    }
}
