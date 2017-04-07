// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using Xunit;

namespace CharLS
{
    public class JpegStreamReaderTest
    {
        [Fact]
        public void ReadHeaderFromToSmallInputBuffer()
        {
            byte[] buffer = new byte[0];

            ByteStreamInfo byteStream = new ByteStreamInfo(buffer, 0);
            JpegStreamReader reader = new JpegStreamReader(byteStream);

            try
            {
                reader.ReadHeader();
            }
            catch (charls_error error)
            {
                Assert.Equal(ApiResult.CompressedBufferTooSmall, error.Code);
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void ReadHeaderFromBufferPrecededWithFillBytes()
        {
            byte[] buffer = { 0xFF, 0xFF, 0xD8, 0xFF, 0xFF, 0xDA }; // SOS: Marks the start of scan.

            ByteStreamInfo byteStream = new ByteStreamInfo(buffer, 6);
            JpegStreamReader reader = new JpegStreamReader(byteStream);

            var exception = Record.Exception(() => reader.ReadHeader()); // if it doesn´t throw test is passed.
            Assert.Null(exception);
        }

        [Fact]
        public void ReadHeaderFromBufferNotStartingWithFFShouldThrow()
        {
            byte[] buffer = { 0x0F, 0xFF, 0xD8, 0xFF, 0xFF, 0xDA }; // SOS: Marks the start of scan.

            ByteStreamInfo byteStream = new ByteStreamInfo(buffer, 6);
            JpegStreamReader reader = new JpegStreamReader(byteStream);

            try
            {
                reader.ReadHeader();
            }
            catch (charls_error error)
            {
                Assert.Equal(ApiResult.MissingJpegMarkerStart, error.Code);
                return;
            }

            Assert.True(false);
        }
    }
}