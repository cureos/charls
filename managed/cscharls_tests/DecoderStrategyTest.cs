// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using Xunit;

namespace CharLS
{
    internal class DecoderStrategyTester<TSample, TPixel> : DecoderStrategy<TSample, TPixel> where TSample : struct
    {
        public DecoderStrategyTester(ITraits<TSample, TPixel> inTraits, JlsParameters parameters, byte[] pOutBuf,
            int nOutBufLen) : base(inTraits, parameters)
        {
            var stream = new ByteStreamInfo(pOutBuf, nOutBufLen);
            Init(stream);
        }

        public override void DecodeScan(IProcessLine processLine, JlsRect rect, ByteStreamInfo compressedData)
        {
        }

        public override void SetPresets(JpegLSPresetCodingParameters presets)
        {
        }

        public override IProcessLine CreateProcess(ByteStreamInfo info)
        {
            return null;
        }

        internal int Read(int length)
        {
            return ReadLongValue(length);
        }

        internal void Finish()
        {
            EndScan();
        }
    }

    public class DecoderStrategyTest
    {
        private struct InData
        {
            internal readonly int val;
            internal readonly int bits;

            internal InData(int v, int b)
            {
                val = v;
                bits = b;
            }
        }

        [Fact]
        public void DecodeEncodedFFPattern()
        {
            var inData = new InData[]
            {
                new InData(0x00, 24), new InData(0xFF, 8), new InData(0xFFFF, 16), new InData(0xFFFF, 16),
                new InData(0x12345678, 31)
            };

            var encBuf = new byte[100];
            var parameters = new JlsParameters();

            var encoder = new EncoderStrategyTester<byte, byte>(new DefaultTraitsT<byte, byte>(255, 0), parameters);

            var stream = new ByteStreamInfo(encBuf);
            encoder.InitForward(stream);

            for (int i = 0; i < inData.Length; i++)
            {
                encoder.AppendToBitStreamForward(inData[i].val, inData[i].bits);
            }
            encoder.EndScanForward();
            // Note: Correct encoding is tested in EncoderStrategyTest::AppendToBitStreamFFPattern.

            var length = (int)encoder.GetLengthForward();
            var dec = new DecoderStrategyTester<byte, byte>(new DefaultTraitsT<byte, byte>(255, 0), parameters, encBuf,
                length);
            for (var i = 0; i < inData.Length; i++)
            {
                var actual = dec.Read(inData[i].bits);
                Assert.Equal(inData[i].val, actual);
            }
        }
    }
}
