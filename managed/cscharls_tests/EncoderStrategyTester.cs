// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    internal class EncoderStrategyTester<TSample, TPixel> : EncoderStrategy<TSample, TPixel> where TSample : struct 
    {
        public EncoderStrategyTester(ITraits<TSample, TPixel> inTraits, JlsParameters parameters) : base(inTraits,
            parameters)
        {
        }

        public override ulong EncodeScan(IProcessLine processLine, ByteStreamInfo compressedData)
        {
            return 0;
        }

        public override void SetPresets(JpegLSPresetCodingParameters presets)
        {
        }

        public override IProcessLine CreateProcess(ByteStreamInfo info)
        {
            return null;
        }

        internal void InitForward(ByteStreamInfo info)
        {
            Init(info);
        }

        internal void AppendToBitStreamForward(int value, int length)
        {
            AppendToBitStream(value, length);
        }

        internal void FlushForward()
        {
            Flush();
        }

        internal ulong GetLengthForward()
        {
            return GetLength();
        }

        internal void EndScanForward()
        {
            EndScan();
        }
    }
}
