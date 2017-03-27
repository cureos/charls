// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.IO;

namespace CharLS
{
    public class PostProcesSingleStream : IProcessLine
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

        public void NewLineDecoded(ArraySegment<byte> pSrc, int pixelCount, int sourceStride)
        {
            if (!_canDecode) throw new InvalidOperationException("Raw data stream does not support writing");
            _rawData.Write(pSrc.Array, pSrc.Offset, pixelCount * _bytesPerPixel);
        }

        public void NewLineRequested(ArraySegment<byte> pDest, int pixelCount, int destStride)
        {
            if (!_canDecode) throw new InvalidOperationException("Raw data stream does not support reading");

            var bytesToRead = pixelCount * _bytesPerPixel;
            var bytesRead = _rawData.Read(pDest.Array, pDest.Offset, bytesToRead);
            if (bytesRead < bytesToRead) throw new charls_error(ApiResult.CompressedBufferTooSmall);

            if (_bytesPerPixel == 2)
            {
                ByteSwap(pDest, bytesToRead);
            }

            if (_bytesPerLine > bytesToRead)
            {
                _rawData.Seek(_bytesPerLine - bytesToRead, SeekOrigin.Current);
            }
        }

        private static void ByteSwap(ArraySegment<byte> data, int count)
        {
            if ((count & 1) != 0)
            {
                var message = $"An odd number of bytes ({count}) cannot be swapped.";
                throw new charls_error(ApiResult.InvalidJlsParameters, message);
            }

            var array = data.Array;
            var begin = data.Offset;
            var end = begin + count;

            for (var i = begin; i < end; i += 2)
            {
                var tmp = array[i];
                array[i] = array[i + 1];
                array[i + 1] = tmp;
            }
        }
    }
}
