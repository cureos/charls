// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using Xunit;

namespace CharLS
{
    public class JpegMarkerSegmentTest
    {
        private static ulong SerializeSegment(IJpegSegment segment, byte[] buffer, int count)
        {
            ByteStreamInfo info = new ByteStreamInfo(buffer, count);
            JpegStreamWriter writer = new JpegStreamWriter();
            writer.AddSegment(segment);
            var bytesWritten = writer.Write(info);

            Assert.True(bytesWritten >= 4);

            // write.Write will always serialize a complete byte stream. Check the leading and trailing JPEG Markers SOI and EOI. 
            Assert.Equal((byte)0xFF, buffer[0]);
            Assert.Equal((byte)0xD8, buffer[1]); // JPEG_SOI

            Assert.Equal((byte)0xFF, buffer[bytesWritten - 2]);
            Assert.Equal((byte)0xD9, buffer[bytesWritten - 1]); // JPEG_EOI

            return bytesWritten;
        }

        [Fact]
        public void CreateStartOfFrameMarkerAndSerialize()
        {
            int bitsPerSample = 8;
            int componentCount = 3;

            var segment = JpegMarkerSegment.CreateStartOfFrameSegment(100, ushort.MaxValue, bitsPerSample, componentCount);

            var buffer = new byte[23];
            var bytesWritten = SerializeSegment(segment, buffer, buffer.Length);

            Assert.Equal((ulong)23, bytesWritten);

            Assert.Equal((byte)0xFF, buffer[2]);
            Assert.Equal((byte)0xF7, buffer[3]); // JPEG_SOF_55
            Assert.Equal((byte)0, buffer[4]);   // 6 + (3 * 3) + 2 (in little endian)
            Assert.Equal((byte)17, buffer[5]);  // 6 + (3 * 3) + 2 (in little endian)
            Assert.Equal((byte)bitsPerSample, buffer[6]);
            Assert.Equal((byte)255, buffer[7]);    // height (in little endian)
            Assert.Equal((byte)255, buffer[8]);  // height (in little endian)
            Assert.Equal((byte)0, buffer[9]);    // width (in little endian)
            Assert.Equal((byte)100, buffer[10]);  // width (in little endian)
            Assert.Equal((byte)componentCount, buffer[11]);

            Assert.Equal((byte)1, buffer[12]);
            Assert.Equal((byte)0x11, buffer[13]);
            Assert.Equal((byte)0, buffer[14]);

            Assert.Equal((byte)2, buffer[15]);
            Assert.Equal((byte)0x11, buffer[16]);
            Assert.Equal((byte)0, buffer[17]);

            Assert.Equal((byte)3, buffer[18]);
            Assert.Equal((byte)0x11, buffer[19]);
            Assert.Equal((byte)0, buffer[20]);
        }

        [Fact]
        public void CreateStartOfFrameMarkerWithLowBoundaryValuesAndSerialize()
        {
            const int bitsPerSample = 2;
            const int componentCount = 1;
            
            var segment = JpegMarkerSegment.CreateStartOfFrameSegment(0, 0, bitsPerSample, componentCount);

            var buffer = new byte[17];
            var bytesWritten = SerializeSegment(segment, buffer, buffer.Length);
            Assert.Equal((ulong)17, bytesWritten);
            Assert.Equal((byte)bitsPerSample, buffer[6]);
            Assert.Equal((byte)componentCount, buffer[11]);
        }

        [Fact]
        public void CreateStartOfFrameMarkerWithHighBoundaryValuesAndSerialize()
        {
            var segment = JpegMarkerSegment.CreateStartOfFrameSegment(ushort.MaxValue, ushort.MaxValue, byte.MaxValue, byte.MaxValue - 1);

            var buffer = new byte[776];
            var bytesWritten = SerializeSegment(segment, buffer, buffer.Length);

            Assert.Equal((ulong)776, bytesWritten);
            Assert.Equal((byte)byte.MaxValue, buffer[6]);
            Assert.Equal((byte)(byte.MaxValue - 1), buffer[11]);
        }

        [Fact]
        public void CreateJpegFileInterchangeFormatMarkerAndSerialize()
        {
            JfifParameters parameters = new JfifParameters();

            parameters.version = 1 * 256 + 2;
            parameters.units = 2;
            parameters.Xdensity = 96;
            parameters.Ydensity = 300;
            parameters.Xthumbnail = 0;
            parameters.Ythumbnail = 0;

            var segment = JpegMarkerSegment.CreateJpegFileInterchangeFormatSegment(parameters);

            var buffer = new byte[22];
            var bytesWritten = SerializeSegment(segment, buffer, buffer.Length);

            Assert.Equal((ulong)22, bytesWritten);

            // Verify JFIF identifier string.
            Assert.Equal((byte)0x4A, buffer[6]);
            Assert.Equal((byte)0x46, buffer[7]);
            Assert.Equal((byte)0x49, buffer[8]);
            Assert.Equal((byte)0x46, buffer[9]);
            Assert.Equal((byte)0, buffer[10]);

            // Verify version
            Assert.Equal((byte)1, buffer[11]);
            Assert.Equal((byte)2, buffer[12]);

            Assert.Equal((byte)parameters.units, buffer[13]);

            // Xdensity
            Assert.Equal((byte)0, buffer[14]);
            Assert.Equal((byte)96, buffer[15]);

            // Ydensity
            Assert.Equal((byte)1, buffer[16]);
            Assert.Equal((byte)44, buffer[17]);
        }

        [Fact]
        public void CreateJpegLSExtendedParametersMarkerAndSerialize()
        {
            JpegLSPresetCodingParameters parameters = new JpegLSPresetCodingParameters();

            parameters.MaximumSampleValue = 2;
            parameters.Threshold1 = 1;
            parameters.Threshold2 = 2;
            parameters.Threshold3 = 3;
            parameters.ResetValue = 7;

            var segment = JpegMarkerSegment.CreateJpegLSPresetParametersSegment(parameters);

            var buffer = new byte[19];
            var bytesWritten = SerializeSegment(segment, buffer, buffer.Length);
            Assert.Equal((ulong)19, bytesWritten);

            // Parameter ID.
            Assert.Equal((byte)0x1, buffer[6]);

            // MaximumSampleValue
            Assert.Equal((byte)0, buffer[7]);
            Assert.Equal((byte)2, buffer[8]);

            // Threshold1 
            Assert.Equal((byte)0, buffer[9]);
            Assert.Equal((byte)1, buffer[10]);

            // Threshold2
            Assert.Equal((byte)0, buffer[11]);
            Assert.Equal((byte)2, buffer[12]);

            // Threshold3
            Assert.Equal((byte)0, buffer[13]);
            Assert.Equal((byte)3, buffer[14]);

            // ResetValue
            Assert.Equal((byte)0, buffer[15]);
            Assert.Equal((byte)7, buffer[16]);
        }

        [Fact]
        public void CreateColorTransformMarkerAndSerialize()
        {
            ColorTransformation transformation = ColorTransformation.HP1;

            var segment = JpegMarkerSegment.CreateColorTransformSegment(transformation);

            var buffer = new byte[13];
            var bytesWritten = SerializeSegment(segment, buffer, buffer.Length);
            Assert.Equal((ulong)13, bytesWritten);

            // Verify mrfx identifier string.
            Assert.Equal((byte)'m', buffer[6]);
            Assert.Equal((byte)'r', buffer[7]);
            Assert.Equal((byte)'f', buffer[8]);
            Assert.Equal((byte)'x', buffer[9]);

            Assert.Equal((byte)transformation, buffer[10]);
        }

        [Fact]
        public void CreateStartOfScanMarkerAndSerialize()
        {
            var segment = JpegMarkerSegment.CreateStartOfScanSegment(6, 1, 2, InterleaveMode.None);

            var buffer = new byte[14];
            var bytesWritten = SerializeSegment(segment, buffer, buffer.Length);
            Assert.Equal((ulong)14, bytesWritten);

            Assert.Equal((byte)1, buffer[6]); // component count.
            Assert.Equal((byte)6, buffer[7]); // component index.
            Assert.Equal((byte)0, buffer[8]); // table ID.
            Assert.Equal((byte)2, buffer[9]); // NEAR parameter.
            Assert.Equal((byte)0, buffer[10]); // ILV parameter.
            Assert.Equal((byte)0, buffer[11]); // transformation.
        }
    }
}
