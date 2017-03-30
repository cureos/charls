// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public interface ICodecStrategy
    {
        void SetPresets(JpegLSPresetCodingParameters presets);

        IProcessLine CreateProcess(ByteStreamInfo info);
    }

    public interface IEncoderStrategy : ICodecStrategy
    {
        int EncodeScan(IProcessLine processLine, ByteStreamInfo compressedData);
    }

    public interface IDecoderStrategy : ICodecStrategy
    {
        void DecodeScan(IProcessLine processLine, JlsRect rect, ByteStreamInfo compressedData);

    }
}
