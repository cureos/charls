// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public interface IStrategy
    {
        int EncodeScan(IProcessLine processLine, ByteStreamInfo compressedData);

        void DecodeScan(IProcessLine processLine, JlsRect rect, ByteStreamInfo compressedData);
    }
}
