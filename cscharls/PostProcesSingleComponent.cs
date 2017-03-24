// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Runtime.InteropServices;

namespace CharLS
{
    public class PostProcesSingleComponent<TSample> : IProcessLine<TSample>
    {
        private readonly byte[] _rawData;

        private readonly int _bytesPerPixel;

        private readonly int _bytesPerLine;

        private int _position;

        public PostProcesSingleComponent(byte[] rawData, JlsParameters parameters, int bytesPerPixel)
        {
            _rawData = rawData;
            _bytesPerPixel = bytesPerPixel;
            _bytesPerLine = parameters.stride;
            _position = 0;
        }

        public void NewLineDecoded(TSample[] pSrc, int pixelCount, int sourceStride)
        {
            var handle = GCHandle.Alloc(pSrc, GCHandleType.Pinned);
            Marshal.Copy(handle.AddrOfPinnedObject(), _rawData, _position, pixelCount * _bytesPerPixel);
            handle.Free();

            _position += _bytesPerLine;
        }

        public void NewLineRequested(TSample[] pDest, int pixelCount, int destStride)
        {
            var handle = GCHandle.Alloc(pDest, GCHandleType.Pinned);
            Marshal.Copy(_rawData, _position, handle.AddrOfPinnedObject(), pixelCount * _bytesPerPixel);
            handle.Free();

            _position += _bytesPerLine;
        }
    }
}
