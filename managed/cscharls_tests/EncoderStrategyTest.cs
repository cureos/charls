// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using Xunit;

namespace CharLS
{
    public class EncoderStrategyTest
    {
        [Fact]
        public void AppendToBitStreamZeroLength()
        {
            var parameters = new JlsParameters();

            var strategy = new EncoderStrategyTester<byte, byte>(new DefaultTraitsT<byte, byte>(255, 0), parameters);

            var data = new byte[1024];

            var stream = new ByteStreamInfo(data);
            strategy.InitForward(stream);

            strategy.AppendToBitStreamForward(0, 0);
            strategy.FlushForward();
        }

        [Fact]
        public void AppendToBitStreamFFPattern()
        {
            var parameters = new JlsParameters();

            var strategy = new EncoderStrategyTester<byte, byte>(new DefaultTraitsT<byte, byte>(255, 0), parameters);

            var data = new byte[1024];
            data[13] = 0x77; // marker byte to detect overruns.

            ByteStreamInfo stream = new ByteStreamInfo(data);
            strategy.InitForward(stream);

            // We want _isFFWritten == true.
            strategy.AppendToBitStreamForward(0, 24);
            strategy.AppendToBitStreamForward(0xff, 8);

            // We need the buffer filled with set bits.
            strategy.AppendToBitStreamForward(0xffff, 16);
            strategy.AppendToBitStreamForward(0xffff, 16);

            // Buffer is full with FFs and _isFFWritten = true: Flush can only write 30 date bits.
            strategy.AppendToBitStreamForward(0x3, 31);

            strategy.FlushForward();

            // Verify output.
            Assert.Equal((ulong)13, strategy.GetLengthForward());
            Assert.Equal(0x00, data[0]);
            Assert.Equal(0x00, data[1]);
            Assert.Equal(0x00, data[2]);
            Assert.Equal(0xFF, data[3]);
            Assert.Equal(0x7F, data[4]); // extra 0 bit.
            Assert.Equal(0xFF, data[5]);
            Assert.Equal(0x7F, data[6]); // extra 0 bit.
            Assert.Equal(0xFF, data[7]);
            Assert.Equal(0x60, data[8]);
            Assert.Equal(0x00, data[9]);
            Assert.Equal(0x00, data[10]);
            Assert.Equal(0x00, data[11]);
            Assert.Equal(0xC0, data[12]);
            Assert.Equal(0x77, data[13]);
        }
    }
}
