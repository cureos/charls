// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CharLS
{
    public class PostProcesSingleStream<TSample> : IProcessLine<TSample>
    {
        private readonly Stream _rawData;

        private readonly int _bytesPerPixel;

        private readonly int _bytesPerLine;

        private readonly bool _canDecode;

        private readonly bool _canEncode;

        public PostProcesSingleStream(Stream rawData, JlsParameters parameters, int bytesPerPixel)
        {
            _canDecode = rawData.CanWrite;
            _canEncode = rawData.CanSeek && rawData.CanRead;
            if (!_canDecode && !_canEncode)
            {
                throw new ArgumentException("Raw data stream does not support reading nor writing", nameof(rawData));
            }

            _rawData = rawData;
            _bytesPerPixel = bytesPerPixel;
            _bytesPerLine = parameters.stride;
        }

        public void NewLineDecoded(TSample[] pSrc, int pixelCount, int sourceStride)
        {
            if (!_canDecode) throw new InvalidOperationException("Raw data stream does not support writing");

            var bytesToWrite = pixelCount * _bytesPerPixel;
            var tmp = new byte[bytesToWrite];

            var handle = GCHandle.Alloc(pSrc, GCHandleType.Pinned);
            Marshal.Copy(handle.AddrOfPinnedObject(), tmp, 0, bytesToWrite);
            handle.Free();
            
            _rawData.Write(tmp, 0, bytesToWrite);
        }

        public void NewLineRequested(TSample[] pDest, int pixelCount, int destStride)
        {
            if (!_canDecode) throw new InvalidOperationException("Raw data stream does not support reading");

            var bytesToRead = pixelCount * _bytesPerPixel;
            var tmp = new byte[bytesToRead];

            var bytesRead = _rawData.Read(tmp, 0, bytesToRead);
            if (bytesRead < bytesToRead) throw new charls_error(ApiResult.CompressedBufferTooSmall);

            if (_bytesPerPixel == 2)
            {
                ByteSwap(tmp, bytesToRead);
            }

            var handle = GCHandle.Alloc(pDest, GCHandleType.Pinned);
            Marshal.Copy(tmp, 0, handle.AddrOfPinnedObject(), bytesToRead);
            handle.Free();

            if (_bytesPerLine > bytesToRead)
            {
                _rawData.Seek(_bytesPerLine - bytesToRead, SeekOrigin.Current);
            }
        }

        private static void ByteSwap(byte[] data, int count)
        {
            if ((count & 1) != 0)
            {
                var message = $"An odd number of bytes ({count}) cannot be swapped.";
                throw new charls_error(ApiResult.InvalidJlsParameters, message);
            }

            for (var i = 0; i < count; i += 2)
            {
                var tmp = data[i];
                data[i] = data[i + 1];
                data[i + 1] = tmp;
            }
        }
    }
}
