// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public interface IStrategy<out TSample> where TSample : struct
    {
        JlsParameters Parameters { get; set; }

        void OnLineBegin(int cpixel, byte[] ptypeBuffer, int pixelStride);

        void OnLineEnd(int cpixel, byte[] ptypeBuffer, int pixelStride);

        void Init(ByteStreamInfo compressedStream);

        void EndScan();

        // Encode/decode a single sample. Performancewise the #1 important functions
        TSample DoRegular(int Qs, int x, int pred);
    }
}